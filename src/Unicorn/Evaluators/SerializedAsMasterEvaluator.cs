using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow;
using Rainbow.Diff;
using Rainbow.Filtering;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Logging;
using Unicorn.UI.Pipelines.GetContentEditorWarnings;

namespace Unicorn.Evaluators
{
	/// <summary>
	/// Evaluates to overwrite the source data if ANY differences exist in the serialized version.
	/// </summary>
	public class SerializedAsMasterEvaluator : NewItemOnlyEvaluator
	{
		private readonly ILogger _globalLogger;
		private readonly ISerializedAsMasterEvaluatorLogger _logger;
		private readonly IItemComparer _itemComparer;
		private readonly IFieldFilter _fieldFilter;
		private readonly ISourceDataStore _sourceDataStore;
		protected static readonly Guid RootId = new Guid("{11111111-1111-1111-1111-111111111111}");

		public SerializedAsMasterEvaluator(ILogger globalLogger, ISerializedAsMasterEvaluatorLogger logger, IItemComparer itemComparer, IFieldFilter fieldFilter, ISourceDataStore sourceDataStore, ITargetDataStore targetDataStore) : base(logger, sourceDataStore, targetDataStore)
		{
			Assert.ArgumentNotNull(globalLogger, "globalLogger");
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(itemComparer, "itemComparer");
			Assert.ArgumentNotNull(fieldFilter, "fieldFilter");
			Assert.ArgumentNotNull(sourceDataStore, "sourceDataStore");

			_globalLogger = globalLogger;
			_logger = logger;
			_itemComparer = itemComparer;
			_fieldFilter = fieldFilter;
			_sourceDataStore = sourceDataStore;
		}

		public override void EvaluateOrphans(IItemData[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");

			foreach (var item in orphanItems)
			{
				RecycleItem(item);
			}
		}

		public override IItemData EvaluateUpdate(IItemData sourceItem, IItemData targetItem)
		{
			Assert.ArgumentNotNull(targetItem, "targetItemData");
			Assert.ArgumentNotNull(sourceItem, "sourceItemData");

			var deferredUpdateLog = new DeferredLogWriter<ISerializedAsMasterEvaluatorLogger>();

			_logger.Evaluated(sourceItem ?? targetItem);

			if (ShouldUpdateExisting(sourceItem, targetItem, deferredUpdateLog))
			{
				using (new LogTransaction(_globalLogger))
				{
					_logger.SerializedUpdatedItem(targetItem);
					deferredUpdateLog.ExecuteDeferredActions(_logger);

					_sourceDataStore.Save(targetItem);
				}
				return targetItem;
			}

			return null;
		}

		public override Warning EvaluateEditorWarning(Item item)
		{
			bool transparentSync = item.Statistics.UpdatedBy == UnicornDataProvider.TransparentSyncUpdatedByValue;
			string title = transparentSync ? "This item is included by Unicorn Transparent Sync" : "This item is controlled by Unicorn";

			string message = "You should not change this item because your changes will be overwritten by the next code deployment. Ask a developer for help if you need to change this item.";

			if (Settings.GetBoolSetting("Unicorn.DevMode", true))
			{
				message = "Changes to this item will be written to disk so they can be committed to source control and shared with others.";
			}

			if (transparentSync)
			{
				using (new TransparentSyncDisabler())
				{
					using (new DatabaseCacheDisabler())
					{
						var dbItem = Database.GetItem(item.Uri);
						if (dbItem != null) message += " Item exists in the Sitecore database as well as Transparent Sync.";
						else
							message += " Item does not exist in the Sitecore database.";
					}
				}
			}

			return new Warning(title, message);
		}

		protected virtual bool ShouldUpdateExisting(IItemData sourceItem, IItemData targetItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			Assert.ArgumentNotNull(targetItem, "targetItem");
			Assert.ArgumentNotNull(sourceItem, "sourceItem");

			if (sourceItem.Id == RootId) return false; // we never want to update the Sitecore root item

			// filter out ignored fields before we do the comparison
			var filteredTargetItem = new FilteredItem(targetItem, _fieldFilter);
			var filteredSourceItem = new FilteredItem(sourceItem, _fieldFilter);

			var comparison = _itemComparer.FastCompare(filteredSourceItem, filteredTargetItem);

			if (comparison.IsRenamed || comparison.IsMoved)
			{
				deferredUpdateLog.AddEntry(log => log.Renamed(sourceItem, targetItem));
			}
			if (comparison.IsTemplateChanged)
			{
				deferredUpdateLog.AddEntry(log => log.TemplateChanged(sourceItem, targetItem));
			}
			foreach (var sharedChange in comparison.ChangedSharedFields)
			{
				deferredUpdateLog.AddEntry(log => log.SharedFieldIsChanged(targetItem, (sharedChange.TargetField ?? sharedChange.SourceField).FieldId, ((sharedChange.TargetField != null) ? sharedChange.TargetField.Value : null), ((sharedChange.SourceField != null) ? sharedChange.SourceField.Value : null)));
			}
			foreach (var versionChange in comparison.ChangedVersions)
			{
				if (versionChange.SourceVersion == null) deferredUpdateLog.AddEntry(log => log.NewTargetVersion(versionChange.TargetVersion, targetItem, sourceItem));
				else if (versionChange.TargetVersion == null) deferredUpdateLog.AddEntry(log => log.OrphanSourceVersion(sourceItem, targetItem, new[] { versionChange.SourceVersion }));
				else
				{
					foreach (var field in versionChange.ChangedFields)
					{
						var sourceFieldValue = field.SourceField == null ? null : field.SourceField.Value;
						var targetFieldValue = field.TargetField == null ? null : field.TargetField.Value;
						var fieldId = (field.SourceField ?? field.TargetField).FieldId;

						deferredUpdateLog.AddEntry(log => log.VersionedFieldIsChanged(targetItem, versionChange.SourceVersion ?? versionChange.TargetVersion, fieldId, targetFieldValue, sourceFieldValue));
					}
				}
			}

			return !comparison.AreEqual;
		}

		/// <summary>
		/// Recycles a whole tree of items and reports their progress
		/// </summary>
		/// <param name="items">The item(s) to delete. Note that their children will be deleted before them, and also be reported upon.</param>
		protected virtual void RecycleItems(IEnumerable<IItemData> items)
		{
			Assert.ArgumentNotNull(items, "items");

			foreach (var item in items)
			{
				RecycleItem(item);
			}
		}

		/// <summary>
		/// Deletes an item from the source data provider
		/// </summary>
		protected virtual void RecycleItem(IItemData itemData)
		{
			var children = _sourceDataStore.GetChildren(itemData);

			EvaluateOrphans(children.ToArray());

			_logger.RecycledItem(itemData);
			_logger.Evaluated(itemData);
			_sourceDataStore.Remove(itemData);
		}

		public override string FriendlyName
		{
			get { return "Serialized as Master Evaluator"; }
		}

		public override string Description
		{
			get { return "Treats the items that are serialized as the master copy, and any changes whether newer or older are synced into the source data. This allows for all merging to occur in source control, and is the default way Unicorn behaves."; }
		}

		public override KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new[] { new KeyValuePair<string, string>("Item comparer", DocumentationUtility.GetFriendlyName(_itemComparer)) };
		}
	}
}
