using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Save;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using Unicorn.Data;
using Unicorn.Predicates;
using Unicorn.Serialization;
using Unicorn.Serialization.Sitecore;

namespace Unicorn
{
	/// <summary>
	/// Provides a saveUI pipeline implementation to prevent unintentionally overwriting a changed serialized item on disk.
	/// 
	/// For example, if user A changes an item, then user B pulls that change from SCM but does not sync it into Sitecore,
	/// then user B changes that item, without this handler user A's changes would be overwritten.
	/// 
	/// This handler verifies that the pre-save state of the item matches the field values present in the serialized item on disk (if it's present).
	/// If they dont match a sheer warning is shown.
	/// </summary>
	/// <remarks>
	/// Note that this solution does NOT save you from every possible conflict situation. Some saves do not run the saveUI pipeline, for example:
	/// - Renaming any item
	/// - Moving items
	/// - Changing template field values (source, type) in the template builder
	/// 
	/// This handler is here to attempt to keep you from shooting yourself in the foot, but you really need to sync Unicorn after every update pulled from SCM.
	/// </remarks>
	public class SerializationConflictProcessor
	{
		private readonly ISerializationProvider _serializationProvider;
		private readonly IPredicate _predicate;

		public SerializationConflictProcessor() : this(new SitecoreSerializationProvider(), new SerializationPresetPredicate(new SitecoreSourceDataProvider()))
		{
			
		}

		public SerializationConflictProcessor(ISerializationProvider serializationProvider, IPredicate predicate)
		{
			_serializationProvider = serializationProvider;
			_predicate = predicate;
		}

		public void Process(SaveArgs args)
		{
			Assert.ArgumentNotNull(args, "args");

			// we had errors, and we got a post-back result of no, don't overwrite
			if (args.Result == "no" || args.Result == "undefined")
			{
				args.SaveAnimation = false;
				args.AbortPipeline();
				return;
			}

			// we had errors, and we got a post-back result of yes, allow overwrite
			if (args.IsPostBack) return;

			string error = GetErrorValue(args);

			// no errors detected, we're good
			if (string.IsNullOrEmpty(error)) return;

			Sitecore.Web.UI.Sheer.SheerResponse.Confirm(error);
			args.WaitForPostBack();
		}

		private string GetErrorValue(SaveArgs args)
		{
			var results = new Dictionary<Item, IList<FieldDesynchronization>>();

			try
			{
				foreach (var item in args.Items)
				{
					Item existingItem = Client.ContentDatabase.GetItem(item.ID, item.Language, item.Version);

					Assert.IsNotNull(existingItem, "Existing item {0} did not exist! This should never occur.", item.ID);

					var existingSitecoreItem = new SitecoreSourceItem(existingItem);

					// ignore conflicts on items that Unicorn is not managing
					if (!_predicate.Includes(existingSitecoreItem).IsIncluded) continue;

					ISerializedReference serializedReference = _serializationProvider.GetReference(existingSitecoreItem);
					
					if(serializedReference == null) continue;

					// not having an existing serialized version means no possibility of conflict here
					ISerializedItem serializedItem = _serializationProvider.GetItem(serializedReference);

					if (serializedItem == null) continue;

					var fieldIssues = GetFieldSyncStatus(existingSitecoreItem, serializedItem);

					if (fieldIssues.Count == 0) continue;

					results.Add(existingItem, fieldIssues);
				}

				// no problems
				if (results.Count == 0) return null;

				var sb = new StringBuilder();
				sb.Append("CRITICAL MESSAGE FROM UNICORN:\n");
				sb.Append("You need to run a Unicorn sync. The following fields did not match the serialized version:\n");

				foreach (var item in results)
				{
					if(results.Count > 1)
						sb.AppendFormat("\n{0}: {1}", item.Key.DisplayName, string.Join(", ", item.Value.Select(x => x.FieldName)));
					else
						sb.AppendFormat("\n{0}", string.Join(", ", item.Value.Select(x => x.FieldName)));
				}

				sb.Append("\n\nDo you want to overwrite anyway?\nTHIS MAY CAUSE LOST WORK.");

				return sb.ToString();
			}
			catch (Exception ex)
			{
				Log.Error("Exception occurred while performing serialization conflict check!", ex, this);
				return "Exception occurred: " + ex.Message; // this will cause a few retries
			}
		}

		private IList<FieldDesynchronization> GetFieldSyncStatus(SitecoreSourceItem item, ISerializedItem serializedItem)
		{
			var desyncs = new List<FieldDesynchronization>();

			var serializedVersion = serializedItem.Versions.FirstOrDefault(x => x.VersionNumber == item.InnerItem.Version.Number && x.Language == item.InnerItem.Language.Name);
			
			if (serializedVersion == null)
			{
				desyncs.Add(new FieldDesynchronization("Version"));
				return desyncs;
			}

			item.InnerItem.Fields.ReadAll();

			foreach (Field field in item.InnerItem.Fields)
			{
				if (field.ID == FieldIDs.Revision || field.ID == FieldIDs.Updated || field.ID == FieldIDs.Created || field.ID == FieldIDs.CreatedBy || field.ID == FieldIDs.UpdatedBy) continue; 
				// we're doing a data comparison here - revision, created (by), updated (by) don't matter
				// skipping these fields allows us to ignore spurious saves the template builder makes to unchanged items being conflicts
			
				// find the field in the serialized item in either versioned or shared fields
				string serializedField;
				
				if(!serializedVersion.Fields.TryGetValue(field.ID.ToString(), out serializedField))
					serializedItem.SharedFields.TryGetValue(field.ID.ToString(), out serializedField);

				// we ignore if the field doesn't exist in the serialized item. This is because if you added a field to a template,
				// that does not immediately re-serialize all items based on that template so it's likely innocuous - we're not overwriting anything.
				if (serializedField == null) continue;

				if (!serializedField.Equals(field.Value, StringComparison.Ordinal))
				{
					desyncs.Add(new FieldDesynchronization(field.Name));
				}
			}

			return desyncs;
		}

		private class FieldDesynchronization
		{
			public FieldDesynchronization(string fieldName)
			{
				FieldName = fieldName;
			}

			public string FieldName { get; private set; }
		}
	}
}
