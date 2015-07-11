using System;
using System.Collections.Generic;
using Rainbow.Diff;
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
		private readonly ISourceDataStore _sourceDataStore;
		private readonly IDeserializer _deserializer;
		protected static readonly Guid RootId = new Guid("{11111111-1111-1111-1111-111111111111}");

		public SerializedAsMasterEvaluator(ISerializedAsMasterEvaluatorLogger logger, IItemComparer itemComparer, ISourceDataStore sourceDataStore, IDeserializer deserializer)
		{
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(itemComparer, "fieldFilter");
			Assert.ArgumentNotNull(itemComparer, "fieldPredicate");

			_logger = logger;
			_itemComparer = itemComparer;
			_sourceDataStore = sourceDataStore;
			_deserializer = deserializer;
		}

		public void EvaluateOrphans(ISerializableItem[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");

			EvaluatorUtility.RecycleItems(orphanItems, _sourceDataStore, item => _logger.DeletedItem(item));
		}

		public ISerializableItem EvaluateNewSerializedItem(ISerializableItem newItem)
		{
			Assert.ArgumentNotNull(newItem, "newItem");

			_logger.DeserializedNewItem(newItem);

			var updatedItem = DoDeserialization(newItem);

			return updatedItem;
		}

		public ISerializableItem EvaluateUpdate(ISerializableItem serializedItem, ISerializableItem existingItem)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(existingItem, "existingItem");

			var deferredUpdateLog = new DeferredLogWriter<ISerializedAsMasterEvaluatorLogger>();

			if (ShouldUpdateExisting(serializedItem, existingItem, deferredUpdateLog))
			{
				_logger.SerializedUpdatedItem(serializedItem);

				deferredUpdateLog.ExecuteDeferredActions(_logger);

				var updatedItem = DoDeserialization(serializedItem);

				return updatedItem;
			}

			return null;
		}

		protected virtual bool ShouldUpdateExisting(ISerializableItem serializedItem, ISerializableItem existingItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(existingItem, "existingItem");

			if (existingItem.Id == RootId) return false; // we never want to update the Sitecore root item

			var comparisonResult = _itemComparer.Compare(serializedItem, existingItem);

			return !comparisonResult.AreEqual;
		}

		protected virtual ISerializableItem DoDeserialization(ISerializableItem serializedItem)
		{
			ISerializableItem updatedItem = _deserializer.Deserialize(serializedItem, false);

			Assert.IsNotNull(updatedItem, "Do not return null from DeserializeItem() - throw an exception if an error occurs.");

			return updatedItem;
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
