using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Diff;
using Rainbow.Filtering;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Diagnostics;
using Unicorn.ControlPanel;
using Unicorn.Data;
using Unicorn.Logging;

namespace Unicorn.Evaluators
{
	/// <summary>
	/// Evaluates to overwrite the source data if ANY differences exist in the serialized version.
	/// </summary>
	public class SerializedAsMasterEvaluator : IEvaluator, IDocumentable
	{
		private readonly ISerializedAsMasterEvaluatorLogger _logger;
		private readonly IItemComparer _itemComparer;
		private readonly IFieldFilter _fieldFilter;
		private readonly ISourceDataStore _sourceDataStore;
		private readonly IDeserializer _deserializer;
		protected static readonly Guid RootId = new Guid("{11111111-1111-1111-1111-111111111111}");

		public SerializedAsMasterEvaluator(ISerializedAsMasterEvaluatorLogger logger, IItemComparer itemComparer, IFieldFilter fieldFilter, ISourceDataStore sourceDataStore, IDeserializer deserializer)
		{
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(itemComparer, "itemComparer");
			Assert.ArgumentNotNull(fieldFilter, "fieldFilter");
			Assert.ArgumentNotNull(sourceDataStore, "sourceDataStore");
			Assert.ArgumentNotNull(deserializer, "deserializer");

			_logger = logger;
			_itemComparer = itemComparer;
			_fieldFilter = fieldFilter;
			_sourceDataStore = sourceDataStore;
			_deserializer = deserializer;
		}

		public void EvaluateOrphans(IItemData[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");

			EvaluatorUtility.RecycleItems(orphanItems, _sourceDataStore, item => _logger.DeletedItem(item));
		}

		public IItemData EvaluateNewSerializedItem(IItemData newItemData)
		{
			Assert.ArgumentNotNull(newItemData, "newItem");

			_logger.DeserializedNewItem(newItemData);

			var updatedItem = DoDeserialization(newItemData);

			return updatedItem;
		}

		public IItemData EvaluateUpdate(IItemData sourceItem, IItemData targetItem)
		{
			Assert.ArgumentNotNull(targetItem, "targetItemData");
			Assert.ArgumentNotNull(sourceItem, "sourceItemData");

			var deferredUpdateLog = new DeferredLogWriter<ISerializedAsMasterEvaluatorLogger>();

			if (ShouldUpdateExisting(sourceItem, targetItem, deferredUpdateLog))
			{
				_logger.SerializedUpdatedItem(targetItem);

				deferredUpdateLog.ExecuteDeferredActions(_logger);

				var updatedItem = DoDeserialization(targetItem);

				return updatedItem;
			}

			return null;
		}

		protected virtual bool ShouldUpdateExisting(IItemData sourceItem, IItemData targetItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			Assert.ArgumentNotNull(targetItem, "targetItem");
			Assert.ArgumentNotNull(sourceItem, "sourceItem");

			if (sourceItem.Id == RootId) return false; // we never want to update the Sitecore root item

			// filter out ignored fields before we do the comparison
			var filteredTargetItem = new FilteredItem(targetItem, _fieldFilter);
			var filteredSourceItem = new FilteredItem(sourceItem, _fieldFilter);

			return !_itemComparer.SimpleCompare(filteredSourceItem, filteredTargetItem);
		}

		protected virtual IItemData DoDeserialization(IItemData targetItem)
		{
			IItemData updatedItemData = _deserializer.Deserialize(targetItem, false);

			Assert.IsNotNull(updatedItemData, "Do not return null from DeserializeItem() - throw an exception if an error occurs.");

			return updatedItemData;
		}

		public string FriendlyName
		{
			get { return "Serialized as Master Evaluator"; }
		}

		public string Description
		{
			get { return "Treats the items that are serialized as the master copy, and any changes whether newer or older are synced into the source data. This allows for all merging to occur in source control, and is the default way Unicorn behaves."; }
		}

		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return null;
		}
	}
}
