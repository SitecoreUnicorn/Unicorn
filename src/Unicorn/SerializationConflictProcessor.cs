using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rainbow.Diff;
using Rainbow.Filtering;
using Rainbow.Model;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Save;
using Sitecore.Web.UI.Sheer;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Predicates;
using ItemData = Rainbow.Storage.Sc.ItemData;

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
		private readonly IConfiguration[] _configurations;

		public SerializationConflictProcessor()
			: this(UnicornConfigurationManager.Configurations)
		{
		}

		protected SerializationConflictProcessor(IConfiguration[] configurations)
		{
			_configurations = configurations;
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

			SheerResponse.Confirm(error);
			args.WaitForPostBack();
		}

		private string GetErrorValue(SaveArgs args)
		{
			var results = new Dictionary<Item, IList<string>>();

			try
			{
				foreach (var item in args.Items)
				{
					Item existingItem = Client.ContentDatabase.GetItem(item.ID, item.Language, item.Version);

					Assert.IsNotNull(existingItem, "Existing item {0} did not exist! This should never occur.", item.ID);

					var existingSitecoreItem = new ItemData(existingItem);

					foreach (var configuration in _configurations)
					{
						// ignore conflicts on items that Unicorn is not managing
						if (!configuration.Resolve<IPredicate>().Includes(existingSitecoreItem).IsIncluded) continue;

						IItemData serializedItemData = configuration.Resolve<ITargetDataStore>().GetByMetadata(existingSitecoreItem, existingSitecoreItem.DatabaseName);
					
						// not having an existing serialized version means no possibility of conflict here
						if (serializedItemData == null) continue;

						var fieldFilter = configuration.Resolve<IFieldFilter>();
						var itemComparer = configuration.Resolve<IItemComparer>();

						var fieldIssues = GetFieldSyncStatus(existingSitecoreItem, serializedItemData, fieldFilter, itemComparer);

						if (fieldIssues.Count == 0) continue;

						results.Add(existingItem, fieldIssues);
					}
					
				}

				// no problems
				if (results.Count == 0) return null;

				var sb = new StringBuilder();
				sb.Append("CRITICAL MESSAGE FROM UNICORN:\n");
				sb.Append("You need to run a Unicorn sync. The following fields did not match the serialized version:\n");

				foreach (var item in results)
				{
					if(results.Count > 1)
						sb.AppendFormat("\n{0}: {1}", item.Key.DisplayName, string.Join(", ", item.Value));
					else
						sb.AppendFormat("\n{0}", string.Join(", ", item.Value));
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

		private IList<string> GetFieldSyncStatus(IItemData itemData, IItemData serializedItemData, IFieldFilter fieldFilter, IItemComparer itemComparer)
		{
			var comparison = itemComparer.Compare(new FilteredItem(itemData, fieldFilter), new FilteredItem(serializedItemData, fieldFilter));

			var db = Factory.GetDatabase(itemData.DatabaseName);

			var changedFields = comparison.ChangedSharedFields
				.Concat(comparison.ChangedVersions.SelectMany(version => version.ChangedFields))
				.Select(field => db.GetItem(new ID((field.SourceField ?? field.TargetField).FieldId)))
				.Where(field => field != null)
				.Select(field => field.DisplayName)
				.ToList();

			if(comparison.IsMoved) changedFields.Add("Item has been moved");
			if(comparison.IsRenamed) changedFields.Add("Item has been renamed");
			if(comparison.IsTemplateChanged) changedFields.Add("Item's template has changed");
			if(comparison.ChangedVersions.Any(version => version.SourceVersion == null || version.TargetVersion == null)) changedFields.Add("Item versions do not match");

			return changedFields;
		}
	}
}
