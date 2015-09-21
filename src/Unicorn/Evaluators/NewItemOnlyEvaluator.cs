using System.Collections.Generic;
using Rainbow;
using Rainbow.Model;
using Sitecore.Diagnostics;
using Unicorn.Data;

namespace Unicorn.Evaluators
{
	/// <summary>
	/// Evaluates to add new items only. Existing or orphaned items are left alone.
	/// NOTE: Using transparent sync with this evaluator is not a good idea, because the semantics of transparent sync are to read from the serialization store directly.
	/// In other words, transparent sync always acts like SerializedAsMasterEvaluator, and because there is no sync invocation the evaluator is not invoked.
	/// </summary>
	public class NewItemOnlyEvaluator : IEvaluator, IDocumentable
	{
		private readonly INewItemOnlyEvaluatorLogger _logger;
		private readonly ISourceDataStore _sourceDataStore;

		public NewItemOnlyEvaluator(INewItemOnlyEvaluatorLogger logger, ISourceDataStore sourceDataStore)
		{
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(sourceDataStore, "sourceDataStore");

			_logger = logger;
			_sourceDataStore = sourceDataStore;
		}

		public virtual void EvaluateOrphans(IItemData[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");
			foreach(var orphan in orphanItems) _logger.Evaluated(orphan);
		}

		public virtual IItemData EvaluateNewSerializedItem(IItemData newItemData)
		{
			Assert.ArgumentNotNull(newItemData, "newItem");

			_logger.DeserializedNewItem(newItemData);

			_sourceDataStore.Save(newItemData);

			_logger.Evaluated(newItemData);

			return newItemData;
		}

		public virtual IItemData EvaluateUpdate(IItemData sourceItem, IItemData targetItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItemData");
			_logger.Evaluated(sourceItem);

			return null;
		}

		public virtual string FriendlyName
		{
			get { return "New Item Only Evaluator"; }
		}

		public virtual string Description
		{
			get { return "During a sync only items that are not already in the Sitecore database are synced. If an item already exists, it is never modified. Useful for deploying items only once and leaving them alone from then on."; }
		}

		public virtual KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new KeyValuePair<string, string>[0];
		}
	}
}
