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
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Predicates;
using Unicorn.UI.Pipelines.GetContentEditorWarnings;

namespace Unicorn.Evaluators
{
	/// <summary>
	/// Evaluates to add new items and new fields to existing items only. Existing fields on existing items or orphaned items are left alone.
	/// </summary>
	public class AddOnlyEvaluator : IEvaluator, IDocumentable
	{
		protected static readonly Guid RootId = new Guid("{11111111-1111-1111-1111-111111111111}");
		private readonly ILogger _globalLogger;
		private readonly IAddOnlyEvaluatorLogger _logger;
		private readonly IItemComparer _itemComparer;
		private readonly IFieldFilter _fieldFilter;
		private readonly ISourceDataStore _sourceDataStore;
		private readonly IConfiguration _parentConfiguration;
		private readonly Database _master;

		public AddOnlyEvaluator(ILogger globalLogger, IAddOnlyEvaluatorLogger logger, IItemComparer itemComparer, IFieldFilter fieldFilter, ISourceDataStore sourceDataStore, IConfiguration parentConfiguration)
		{
			_globalLogger = globalLogger;
			_logger = logger;
			_itemComparer = itemComparer;
			_fieldFilter = fieldFilter;
			_sourceDataStore = sourceDataStore;
			_parentConfiguration = parentConfiguration;
			_master = Factory.GetDatabase("master");
		}

		public void EvaluateOrphans(IItemData[] orphanItems)
		{
			if (orphanItems == null) throw new ArgumentNullException(nameof(orphanItems));
			foreach (var orphanItem in orphanItems)
			{
				_logger.Evaluated(orphanItem);
			}
		}

		public IItemData EvaluateNewSerializedItem(IItemData newItemData)
		{
			if (newItemData == null) throw new ArgumentNullException(nameof(newItemData));

			_logger.DeserializedNewItem(newItemData);
			_sourceDataStore.Save(newItemData);
			_logger.Evaluated(newItemData);
			return newItemData;
		}

		public IItemData EvaluateUpdate(IItemData sourceItem, IItemData targetItem)
		{
			if (sourceItem == null) throw new ArgumentNullException(nameof(sourceItem));
			if (targetItem == null) throw new ArgumentNullException(nameof(targetItem));

			var deferredUpdateLog = new DeferredLogWriter<ISerializedAsMasterEvaluatorLogger>();
			_logger.Evaluated(sourceItem);

			IItemData mergedItem;
			if (ShouldUpdateExisting(sourceItem, targetItem, deferredUpdateLog, out mergedItem))
			{
				using (new LogTransaction(_globalLogger))
				{
					_logger.SerializedUpdatedItem(targetItem);
					deferredUpdateLog.ExecuteDeferredActions(_logger);

					_sourceDataStore.Save(mergedItem);
					ClearCachesForItem(mergedItem);
					return mergedItem;
				}
			}
			return null;
		}

		public Warning EvaluateEditorWarning(Item item, PredicateResult predicateResult)
		{
			const string title = "This item is controlled by Unicorn";
			var message = "You can make changes this item. It's still possible that when a developer provides a new version for it, fields will be added. This item will however never be deleted.";

			if (Sitecore.Configuration.Settings.GetBoolSetting("Unicorn.DevMode", true))
				message = "Changes to this item will be written to disk as part of the '{0}' configuration so they can be shared with others.".FormatWith((object)this._parentConfiguration.Name);

			return new Warning(title, message);
		}

		public bool ShouldPerformConflictCheck(Item item)
		{
			return false;
		}

		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new[] { new KeyValuePair<string, string>("Item comparer", DocumentationUtility.GetFriendlyName(_itemComparer)) };
		}

		protected virtual bool ShouldUpdateExisting(IItemData sourceItem, IItemData targetItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog, out IItemData mergedItem)
		{
			if (sourceItem == null) throw new ArgumentNullException(nameof(sourceItem));
			if (targetItem == null) throw new ArgumentNullException(nameof(targetItem));

			mergedItem = null;

			if (sourceItem.Id == RootId)
				return false;
			
			var comparisonResult = _itemComparer.FastCompare(new FilteredItem(sourceItem, _fieldFilter), new FilteredItem(targetItem, _fieldFilter));

			if (comparisonResult.IsRenamed || comparisonResult.IsMoved)
			{
				deferredUpdateLog.AddEntry(log => log.Renamed(sourceItem, targetItem));
				return false;
			}

			if (comparisonResult.IsTemplateChanged)
			{
				deferredUpdateLog.AddEntry(log => log.TemplateChanged(sourceItem, targetItem));
				return false;
			}

			var proxyItem = new ProxyItem(sourceItem);

			var hasChanges = false;

			foreach (var sharedField in targetItem.SharedFields)
			{
				if (!sourceItem.SharedFields.Any(x => x.FieldId.Equals(sharedField.FieldId)))
				{
					hasChanges = true;
					proxyItem.SharedFields = sourceItem.SharedFields.Union(new[] { sharedField }).ToArray();
				}
			}

			var hasChangedPerLanguage = false;
			var unversionedProxyFields = new List<IItemLanguage>();
			foreach (var unversionedFields in targetItem.UnversionedFields)
			{
				var language = unversionedFields.Language;
				var unversionedProxyField = new ProxyItemLanguage(language)
				{
					Fields = sourceItem.UnversionedFields.Where(x => x.Language.Equals(language))
						.SelectMany(x => x.Fields).ToArray()
				};
				foreach (var unversionedField in unversionedFields.Fields)
				{
					if (!unversionedProxyField.Fields.Any(x => x.FieldId.Equals(unversionedField.FieldId)))
					{
						hasChangedPerLanguage = true;
						unversionedProxyField.Fields = unversionedProxyField.Fields.Union(new[] { unversionedField }).ToArray();
					}
				}
				unversionedProxyFields.Add(unversionedProxyField);
			}
			if (proxyItem.UnversionedFields.Count() < unversionedProxyFields.Count || hasChangedPerLanguage)
			{
				hasChanges = true;
				proxyItem.UnversionedFields = unversionedProxyFields.ToArray();
			}

			var hasChangesPerVersion = false;
			var versionedProxyFields = new List<IItemVersion>();
			foreach (var versionedFields in targetItem.Versions)
			{
				var language = versionedFields.Language;
				var version = versionedFields.VersionNumber;
				var versionedProxyField = new ProxyItemVersion(language, version)
				{
					Fields = sourceItem.Versions.Where(x => x.Language.Equals(language) && x.VersionNumber.Equals(version))
						.SelectMany(x => x.Fields).ToArray()
				};
				foreach (var versionedField in versionedFields.Fields)
				{
					if (!versionedProxyField.Fields.Any(x => x.FieldId.Equals(versionedField.FieldId)))
					{
						hasChangesPerVersion = true;
						versionedProxyField.Fields = versionedProxyField.Fields.Union(new[] { versionedField }).ToArray();
					}
				}
				versionedProxyFields.Add(versionedProxyField);
			}
			if (proxyItem.Versions.Count() < versionedProxyFields.Count || hasChangesPerVersion)
			{
				hasChanges = true;
				proxyItem.Versions = versionedProxyFields.ToArray();
			}

			if (!hasChanges)
				return false;

			mergedItem = proxyItem;
			return true;
		}

		private void ClearCachesForItem(IItemData itemData)
		{
			_master.Caches.ItemCache.RemoveItem(ID.Parse(itemData.Id));
			_master.Caches.DataCache.RemoveItemInformation(ID.Parse(itemData.Id));
		}

		public string FriendlyName => "Add Only Evaluator";
		public string Description => "During a Sync, items that do not yet exist in Sitecore will be created, and fields that do not yet exist on existing items will be added.";
	}
}
