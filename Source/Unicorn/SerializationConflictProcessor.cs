using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Save;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Fields;

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
	public class SerializationConflictProcessor
	{
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

			var results = new Dictionary<Item, IList<FieldDesynchronization>>();

			foreach (var item in args.Items)
			{
				Item existingItem = Client.ContentDatabase.GetItem(item.ID);
				SyncItem serializedVersion = ReadSerializedVersion(existingItem);

				// not having an existing serialized version means no possibility of conflict here
				if (serializedVersion == null) continue;

				var fieldIssues = GetFieldSyncStatus(existingItem, serializedVersion);

				if (fieldIssues.Count == 0) continue;

				results.Add(existingItem, fieldIssues);
			}

			// no problems
			if (results.Count == 0) return;

			var sb = new StringBuilder();
			sb.Append("CRITICAL:\n");
			sb.Append("Item state conflicted with existing serialized value. Chances are you need to sync your database with the filesystem. The following fields had problems:\n");
			foreach (var item in results)
			{
				sb.AppendFormat("\n{0}: {1}", item.Key.DisplayName, string.Join(", ", item.Value.Select(x => x.FieldName)));
			}

			sb.Append("\n\nDo you want to overwrite anyway? You may cause someone else to lose committed work by doing this.");

			Sitecore.Web.UI.Sheer.SheerResponse.Confirm(sb.ToString());		
			args.WaitForPostBack();
		}

		private IList<FieldDesynchronization> GetFieldSyncStatus(Item item, SyncItem serializedItem)
		{
			var desyncs = new List<FieldDesynchronization>();

			var serializedVersion = serializedItem.Versions.FirstOrDefault(x => x.Version == item.Version.Number.ToString(CultureInfo.InvariantCulture) && x.Language == item.Language.Name);
			
			if (serializedVersion == null)
			{
				desyncs.Add(new FieldDesynchronization("Version"));
				return desyncs;
			}

			item.Fields.ReadAll();

			foreach (Field field in item.Fields)
			{
				// find the field in the serialized item in either versioned or shared fields
				var serializedField = serializedVersion.Fields.FirstOrDefault(x => x.FieldID == field.ID.ToString()) ??
									serializedItem.SharedFields.FirstOrDefault(x => x.FieldID == field.ID.ToString());

				// we ignore if the field doesn't exist in the serialized item. This is because if you added a field to a template,
				// that does not immediately re-serialize all items based on that template so it's likely innocuous - we're not overwriting anything.
				if (serializedField == null) continue;

				if (!serializedField.FieldValue.Equals(field.Value, StringComparison.Ordinal))
				{
					desyncs.Add(new FieldDesynchronization(serializedField.FieldName));
				}
			}

			return desyncs;
		}

		private SyncItem ReadSerializedVersion(Item item)
		{
			Assert.IsNotNull(item, "Item cannot be null");

			var reference = new ItemReference(item);
			var path = PathUtils.GetFilePath(reference.ToString());

			if (!File.Exists(path)) return null;

			using (var file = new StreamReader(path))
			{
				 return SyncItem.ReadItem(new Tokenizer(file));
			}
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
