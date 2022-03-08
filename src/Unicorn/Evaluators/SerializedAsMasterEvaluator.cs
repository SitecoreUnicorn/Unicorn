using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rainbow;
using Rainbow.Diff;
using Rainbow.Filtering;
using Rainbow.Model;
using Rainbow.Storage;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Logging;
using Unicorn.Predicates;
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
		private readonly ITargetDataStore _targetDataStore;
		private readonly IConfiguration _parentConfiguration;
		protected static readonly Guid RootId = new Guid("{11111111-1111-1111-1111-111111111111}");

		public SerializedAsMasterEvaluator(ILogger globalLogger, ISerializedAsMasterEvaluatorLogger logger, IItemComparer itemComparer, IFieldFilter fieldFilter, ISourceDataStore sourceDataStore, ITargetDataStore targetDataStore, IConfiguration parentConfiguration) : base(logger, sourceDataStore, targetDataStore, parentConfiguration)
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
			_targetDataStore = targetDataStore;
			_parentConfiguration = parentConfiguration;
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

			var result = _parentConfiguration.Resolve<IPredicate>().Includes(targetItem);

			// TODO: In reality, `result` should never come back null here. With the current tests it does however, and it's
			// ^&*"$*£"&(* to change them
			if (ShouldUpdateExisting(sourceItem, targetItem, deferredUpdateLog, result?.FieldValueManipulator))
			{
				using (new LogTransaction(_globalLogger))
				{
					var changeHappened = _sourceDataStore.Save(targetItem, result?.FieldValueManipulator);

					if (changeHappened)
					{
						_logger.SerializedUpdatedItem(targetItem);
						deferredUpdateLog.ExecuteDeferredActions(_logger);
					}
				}
				return targetItem;
			}

			return null;
		}

		public override Warning EvaluateEditorWarning(Item item, PredicateResult predicateResult)
		{
			bool transparentSync = item.Statistics.UpdatedBy == UnicornDataProvider.TransparentSyncUpdatedByValue;
			string title = transparentSync ? "This item is included by Unicorn Transparent Sync" : "This item is controlled by Unicorn";

			var message = new StringBuilder();

			if (IsDevMode)
			{
				message.Append("Changes to this item will be written to disk so they can be shared with others.");
			}
			else
			{
				message.Append("<b style=\"color: red; font-size: 24px;\">You should not change this item because your changes will be overwritten by the next code deployment.</b><br>Ask a developer for help if you need to change this item.");
			}

			message.Append($"<br><br><b>Configuration</b>: {_parentConfiguration.Name}");

			if (predicateResult.PredicateComponentId != null)
			{
				message.Append($"<br><b>Predicate Component</b>: {predicateResult.PredicateComponentId}");
			}

			if (transparentSync)
			{
				// it's static. deal with it. :cat:
				var isInDatabase = TransparentSyncStatusChecker.IsInDatabase(item);
				if (isInDatabase)
				{
					message.Append("<br><b>Transparent Sync</b>: Database + Serialized");
				}
				else
				{
					message.Append("<br><b>Transparent Sync</b>: Serialized Only");
				}
			}

			var existingTargetItem = _targetDataStore.GetByPathAndId(item.Paths.Path, item.ID.Guid, item.Database.Name);

			// check if serialized item ID looks like a filesystem path e.g. c:\
			if (IsDevMode && existingTargetItem?.SerializedItemId != null && existingTargetItem.SerializedItemId.Substring(1, 2) == ":\\")
			{
				message.Append($"<br><b>Physical path</b>: <span style=\"font-family: consolas, monospace\">{existingTargetItem.SerializedItemId}</span>");
			}

			var configNode = Sitecore.Configuration.Factory.GetConfigNode("dataProviders/unicorn");
			if (IsDevMode && configNode == null)
			{
				message.Append("<br><br><b style=\"color: red; \">The Unicorn DataProvider is not configured. Changes will not be saved to the local file system. Please enable the appropriate Unicorn.DataProvider.config for your Sitecore version.</b><br>");
				message.Append("<b style=\"color: red; \">See this github <a target=\"blank\" href=\"https://github.com/SitecoreUnicorn/Unicorn/issues/423\">issue</a> for details.</b><br>");
			}

			return new Warning(title, message.ToString());
		}

		public override bool ShouldPerformConflictCheck(Item item)
		{
			// we expect that items on disk should always match the base state of what is being saved, as disk is master
			return true;
		}

		protected virtual bool ShouldUpdateExisting(IItemData sourceItem, IItemData targetItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog, IFieldValueManipulator fieldValueManipulator)
		{
			Assert.ArgumentNotNull(targetItem, "targetItem");
			Assert.ArgumentNotNull(sourceItem, "sourceItem");

			if (sourceItem.Id == RootId) return false; // we never want to update the Sitecore root item

			// Taking a shortcut for now. If there is a dynamic field value manipiulator, it's true result can only really be obtained when doing the _actual_ write, not when trying to second guess if a write is needed
			if (fieldValueManipulator != null)
				return true;

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
				deferredUpdateLog.AddEntry(log => log.SharedFieldIsChanged(
					targetItem,
					(sharedChange.TargetField ?? sharedChange.SourceField).FieldId,
					sharedChange.TargetField?.Value,
					sharedChange.SourceField?.Value));
			}

			foreach (var unversionedChange in comparison.ChangedUnversionedFields)
			{
				foreach (var uvField in unversionedChange.ChangedFields)
				{
					deferredUpdateLog.AddEntry(log => log.UnversionedFieldIsChanged(
						targetItem,
						unversionedChange.Language.Language,
						(uvField.TargetField ?? uvField.SourceField).FieldId,
						uvField.TargetField?.Value,
						uvField.SourceField?.Value));
				}
			}

			foreach (var versionChange in comparison.ChangedVersions)
			{
				if (versionChange.SourceVersion == null) deferredUpdateLog.AddEntry(log => log.NewTargetVersion(versionChange.TargetVersion, targetItem, sourceItem));
				else if (versionChange.TargetVersion == null) deferredUpdateLog.AddEntry(log => log.OrphanSourceVersion(sourceItem, targetItem, new[] { versionChange.SourceVersion }));
				else
				{
					foreach (var field in versionChange.ChangedFields)
					{
						var sourceFieldValue = field.SourceField?.Value;
						var targetFieldValue = field.TargetField?.Value;
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

		public override string FriendlyName => "Serialized as Master Evaluator";

		public override string Description => "Treats the items that are serialized as the master copy, and any changes whether newer or older are synced into the source data. This allows for all merging to occur in source control, and is the default way Unicorn behaves.";

		public override KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new[] { new KeyValuePair<string, string>("Item comparer", DocumentationUtility.GetFriendlyName(_itemComparer)) };
		}
	}
}
