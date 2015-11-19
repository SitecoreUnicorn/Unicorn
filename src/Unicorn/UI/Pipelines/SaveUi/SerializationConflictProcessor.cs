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
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Evaluators;
using Unicorn.Predicates;
using ItemData = Rainbow.Storage.Sc.ItemData;

namespace Unicorn.UI.Pipelines.SaveUi
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
	public class SerializationConflictProcessor : SaveUiConfirmProcessor
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

		protected override string GetDialogText(SaveArgs args)
		{
			var results = new Dictionary<Item, IList<string>>();

			foreach (var item in args.Items)
			{
				// we grab the existing item from the database. This will NOT include the changed values we're saving.
				// this is because we want to verify that the base state of the item matches serialized, NOT the state we're saving.
				// if the base state and the serialized state match we can be pretty sure that the changes we are writing won't clobber anything serialized but not synced
				Item existingItem = Client.ContentDatabase.GetItem(item.ID, item.Language, item.Version);

				Assert.IsNotNull(existingItem, "Existing item {0} did not exist! This should never occur.", item.ID);

				var existingSitecoreItem = new ItemData(existingItem);

				foreach (var configuration in _configurations)
				{
					// ignore conflict checks if Transparent Sync is turned on (in which case this is a tautology - 'get from database' would be 'get from disk' so it always matches
					if (configuration.Resolve<IUnicornDataProviderConfiguration>().EnableTransparentSync) continue;

					// ignore conflicts on items that Unicorn is not managing
					if (!configuration.Resolve<IPredicate>().Includes(existingSitecoreItem).IsIncluded) continue;

					// evaluator signals that it does not care about conflicts (e.g. NIO)
					if (!configuration.Resolve<IEvaluator>().ShouldPerformConflictCheck(existingItem)) continue;

					IItemData serializedItemData = configuration.Resolve<ITargetDataStore>().GetByPathAndId(existingSitecoreItem.Path, existingSitecoreItem.Id, existingSitecoreItem.DatabaseName);

					// not having an existing serialized version means no possibility of conflict here
					if (serializedItemData == null) continue;

					var fieldFilter = configuration.Resolve<IFieldFilter>();
					var itemComparer = configuration.Resolve<IItemComparer>();

					var fieldIssues = GetFieldSyncStatus(existingSitecoreItem, serializedItemData, fieldFilter, itemComparer);

					if (fieldIssues.Count == 0) continue;

					results[existingItem] = fieldIssues;
				}
			}

			// no problems
			if (results.Count == 0) return null;

			var sb = new StringBuilder();

			if (About.Version.StartsWith("7") || About.Version.StartsWith("6"))
			{
				// older Sitecores used \n to format dialog text

				sb.Append("CRITICAL MESSAGE FROM UNICORN:\n");
				sb.Append("You need to run a Unicorn sync. The following fields did not match the serialized version:\n");

				foreach (var item in results)
				{
					if (results.Count > 1)
						sb.AppendFormat("\n{0}: {1}", item.Key.DisplayName, string.Join(", ", item.Value));
					else
						sb.AppendFormat("\n{0}", string.Join(", ", item.Value));
				}

				sb.Append("\n\nDo you want to overwrite anyway?\nTHIS MAY CAUSE LOST WORK.");
			}
			else
			{
				// Sitecore 8.x+ uses HTML to format dialog text
				sb.Append("<p style=\"font-weight: bold; margin-bottom: 1em;\">CRITICAL MESSAGE FROM UNICORN:</p>");
				sb.Append("<p>You need to run a Unicorn sync. The following fields did not match the serialized version:</p><ul style=\"margin: 1em 0\">");

				foreach (var item in results)
				{
					if (results.Count > 1)
						sb.AppendFormat("<li>{0}: {1}</li>", item.Key.DisplayName, string.Join(", ", item.Value));
					else
						sb.AppendFormat("<li>{0}</li>", string.Join(", ", item.Value));
				}

				sb.Append("</ul><p>Do you want to overwrite anyway?<br>THIS MAY CAUSE LOST WORK.</p>");
			}

			return sb.ToString();
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

			if (comparison.IsMoved) changedFields.Add("Item has been moved");
			if (comparison.IsRenamed) changedFields.Add("Item has been renamed");
			if (comparison.IsTemplateChanged) changedFields.Add("Item's template has changed");
			if (comparison.ChangedVersions.Any(version => version.SourceVersion == null || version.TargetVersion == null)) changedFields.Add("Item versions do not match");

			return changedFields;
		}
	}
}
