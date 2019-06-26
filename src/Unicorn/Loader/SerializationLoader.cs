using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Rainbow.Model;
using Sitecore.Caching;
using Sitecore.Data.Events;
using Sitecore.Diagnostics;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Data.Dilithium;
using Unicorn.Evaluators;
using Unicorn.Predicates;
// ReSharper disable TooWideLocalVariableScope

namespace Unicorn.Loader
{
	/// <summary>
	/// The loader is the heart of Unicorn syncing. It encapsulates the logic required to walk the tree according to a predicate and invoke the evaluator to decide what to do with the tree items.
	/// </summary>
	public class SerializationLoader
	{
		private int _itemsProcessed;
		protected readonly ITargetDataStore TargetDataStore;
		protected readonly IPredicate Predicate;
		protected readonly IEvaluator Evaluator;
		protected readonly ISourceDataStore SourceDataStore;
		protected readonly ISerializationLoaderLogger Logger;
		protected readonly ISyncConfiguration SyncConfiguration;
		protected readonly IUnicornDataProviderConfiguration DataProviderConfiguration;
		protected readonly PredicateRootPathResolver PredicateRootPathResolver;

		public SerializationLoader(ISourceDataStore sourceDataStore, ITargetDataStore targetDataStore, IPredicate predicate, IEvaluator evaluator, ISerializationLoaderLogger logger, ISyncConfiguration syncConfiguration, IUnicornDataProviderConfiguration dataProviderConfiguration, PredicateRootPathResolver predicateRootPathResolver)
		{
			Assert.ArgumentNotNull(targetDataStore, nameof(targetDataStore));
			Assert.ArgumentNotNull(sourceDataStore, nameof(sourceDataStore));
			Assert.ArgumentNotNull(predicate, nameof(predicate));
			Assert.ArgumentNotNull(evaluator, nameof(evaluator));
			Assert.ArgumentNotNull(logger, nameof(logger));
			Assert.ArgumentNotNull(predicateRootPathResolver, nameof(predicateRootPathResolver));
			Assert.ArgumentNotNull(syncConfiguration, nameof(syncConfiguration));
			Assert.ArgumentNotNull(dataProviderConfiguration, nameof(dataProviderConfiguration));

			Logger = logger;
			SyncConfiguration = syncConfiguration;
			DataProviderConfiguration = dataProviderConfiguration;
			PredicateRootPathResolver = predicateRootPathResolver;
			Evaluator = evaluator;
			Predicate = predicate;
			TargetDataStore = targetDataStore;
			SourceDataStore = sourceDataStore;
		}

		/// <summary>
		/// Loads all items in the configured predicate
		/// </summary>
		public virtual void LoadAll(IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(retryer, "retryer");

			var roots = PredicateRootPathResolver.GetRootSerializedItems();
			LoadAll(roots, retryer, consistencyChecker);
		}

		public virtual void LoadAll(IItemData[] rootItemsData, IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker, Action<IItemData> rootLoadedCallback = null)
		{
			Assert.ArgumentNotNull(rootItemsData, "rootItems");
			Assert.IsTrue(rootItemsData.Length > 0, "No root items were passed!");

			if (DataProviderConfiguration.EnableTransparentSync)
			{
				CacheManager.ClearAllCaches();
				// BOOM! This clears all caches before we begin; 
				// because for a TpSync configuration we could have TpSync items in the data cache which 'taint' the item comparisons and result in missed updates
			}

			bool disableNewSerialization = UnicornDataProvider.DisableSerialization;
			try
			{
				UnicornDataProvider.DisableSerialization = true;

				using (new EventDisabler())
				{
					foreach (var rootItem in rootItemsData)
					{
						LoadTree(rootItem, retryer, consistencyChecker);
						rootLoadedCallback?.Invoke(rootItem);
					}

					retryer.RetryAll(SourceDataStore, item => DoLoadItem(item, null), item => LoadTreeInternal(item, retryer, null));
				}
			}
			finally
			{
				UnicornDataProvider.DisableSerialization = disableNewSerialization;
			}
		}

		/// <summary>
		/// Loads a tree from serialized items on disk.
		/// </summary>
		protected internal virtual void LoadTree(IItemData rootItemData, IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(rootItemData, "rootItem");
			Assert.ArgumentNotNull(retryer, "retryer");
			Assert.ArgumentNotNull(consistencyChecker, "consistencyChecker");

			_itemsProcessed = 0;
			var timer = new Stopwatch();
			timer.Start();

			Logger.BeginLoadingTree(rootItemData);

			// LoadTreeInternal does not load the root item passed to it (only the children thereof)
			// so we have to seed the load by loading the root item
			using (new UnicornOperationContext())
			{
				try
				{
					DoLoadItem(rootItemData, consistencyChecker);
				}
				catch (Exception exception)
				{
					retryer.AddItemRetry(rootItemData, exception);
				}
			}

			// load children of the root
			LoadTreeInternal(rootItemData, retryer, consistencyChecker);

			Logger.EndLoadingTree(rootItemData, _itemsProcessed, timer.ElapsedMilliseconds);

			timer.Stop();
		}

		/// <summary>
		/// Recursive method that loads a given tree and retries failures already present if any
		/// </summary>
		protected virtual void LoadTreeInternal(IItemData root, IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(retryer, "retryer");

			var included = Predicate.Includes(root);
			if (!included.IsIncluded)
			{
				if (!ReactorContext.IsActive)
				{
					// we skip this when Dilithium is active because it's entirely probable that another config, containing ignored children, may also be in the cache - so we cannot guarantee this log message being accurate.
					Logger.SkippedItemPresentInSerializationProvider(root, Predicate.FriendlyName, TargetDataStore.GetType().Name, included.Justification ?? string.Empty);
				}

				return;
			}

			var processQueue = new Queue<IItemData>();

			// put the root in the queue
			processQueue.Enqueue(root);

			using (new UnicornOperationContext()) // disablers only work on the current thread. So we need to disable on all worker threads
			{
				IItemData parentItem;
				while (processQueue.Count > 0)
				{
					parentItem = processQueue.Dequeue();
					try
					{
						// load the current level
						LoadOneLevel(parentItem, retryer, consistencyChecker);

						// check if we have child paths to process down
						var children = TargetDataStore.GetChildren(parentItem).ToArray();

						if (children.Length > 0)
						{
							// load each child path
							foreach (var child in children)
							{
								processQueue.Enqueue(child);
							}
						} // children.length > 0
					}
					catch (ConsistencyException)
					{
						throw;
					}
					catch (Exception ex)
					{
						retryer.AddTreeRetry(root, ex);
					}
				} // end while
			}
		}

		/// <summary>
		/// Loads a set of children from a serialized path
		/// </summary>
		protected virtual void LoadOneLevel(IItemData rootSerializedItemData, IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(rootSerializedItemData, "root");
			Assert.ArgumentNotNull(retryer, "retryer");

			var orphanCandidates = new Dictionary<Guid, IItemData>();

			// get the corresponding item from Sitecore
			IItemData rootSourceItemData = SourceDataStore.GetByPathAndId(rootSerializedItemData.Path, rootSerializedItemData.Id, rootSerializedItemData.DatabaseName);

			// we add all of the root item's direct children to the "maybe orphan" list (we'll remove them as we find matching serialized children)
			if (rootSourceItemData != null)
			{
				var rootSourceChildren = SourceDataStore.GetChildren(rootSourceItemData);
				foreach (IItemData child in rootSourceChildren)
				{
					// if the preset includes the child add it to the orphan-candidate list (if we don't deserialize it below, it will be marked orphan)
					var included = Predicate.Includes(child);
					if (included.IsIncluded)
						orphanCandidates[child.Id] = child;
					else
					{
						Logger.SkippedItem(child, Predicate.FriendlyName, included.Justification ?? string.Empty);
					}
				}
			}

			// check for direct children of the target path
			var serializedChildren = TargetDataStore.GetChildren(rootSerializedItemData);
			foreach (var serializedChild in serializedChildren)
			{
				try
				{
					// Because the load order is breadth-first, standard values will be loaded PRIOR TO THEIR TEMPLATE FIELDS
					// So if we find a standard values item, we throw it straight onto the retry list, which will make it load last
					// after everything else, ensuring its fields all exist first. (This is also how Sitecore serialization does it...)
					if (serializedChild.Path.EndsWith("__Standard Values", StringComparison.OrdinalIgnoreCase))
					{
						retryer.AddItemRetry(serializedChild, new Exception("Pushing standard values item to the end of loading."));
						orphanCandidates.Remove(serializedChild.Id);
						continue;
					}

					// load a child item
					var loadedSourceItem = DoLoadItem(serializedChild, consistencyChecker);
					if (loadedSourceItem.ItemData != null)
					{
						orphanCandidates.Remove(loadedSourceItem.ItemData.Id);

						// check if we have any child serialized items under this loaded child item (existing children) -
						// if we do not, we can orphan any included children of the loaded item as well
						var loadedItemSerializedChildren = TargetDataStore.GetChildren(serializedChild);

						if (!loadedItemSerializedChildren.Any()) // no children were serialized on disk
						{
							var loadedSourceChildren = SourceDataStore.GetChildren(loadedSourceItem.ItemData);
							foreach (IItemData loadedSourceChild in loadedSourceChildren)
							{
								// place any included source children on the orphan list for deletion, as no serialized children existed
								if (Predicate.Includes(loadedSourceChild).IsIncluded)
									orphanCandidates.Add(loadedSourceChild.Id, loadedSourceChild);
							}
						}
					}
					else if (loadedSourceItem.Status == ItemLoadStatus.Skipped) // if the item got skipped we'll prevent it from being deleted
						orphanCandidates.Remove(serializedChild.Id);
				}
				catch (ConsistencyException)
				{
					throw;
				}
				catch (Exception ex)
				{
					// if a problem occurs we attempt to retry later
					retryer.AddItemRetry(serializedChild, ex);

					// don't treat errors as cause to delete an item
					orphanCandidates.Remove(serializedChild.Id);
				}
			}

			// if we're forcing an update (ie deleting stuff not on disk) we send the items that we found that weren't on disk off to get evaluated as orphans
			if (orphanCandidates.Count > 0)
			{
				Evaluator.EvaluateOrphans(orphanCandidates.Values.ToArray());
			}
		}

		/// <summary>
		/// Loads a specific item from disk
		/// </summary>
		protected virtual ItemLoadResult DoLoadItem(IItemData serializedItemData, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(serializedItemData, "serializedItem");

			if (consistencyChecker != null)
			{
				if (!consistencyChecker.IsConsistent(serializedItemData)) throw new ConsistencyException("Consistency check failed - aborting loading.");
				consistencyChecker.AddProcessedItem(serializedItemData);
			}

			_itemsProcessed++;

			var included = Predicate.Includes(serializedItemData);

			if (!included.IsIncluded)
			{
				if (!ReactorContext.IsActive)
				{
					// we skip this when Dilithium is active because it's entirely probable that another config, containing ignored children, may also be in the cache - so we cannot guarantee this log message being accurate.
					Logger.SkippedItemPresentInSerializationProvider(serializedItemData, Predicate.FriendlyName, TargetDataStore.FriendlyName, included.Justification ?? string.Empty);
				}
				return new ItemLoadResult(ItemLoadStatus.Skipped);
			}

			// detect if we should run an update for the item or if it's already up to date
			var existingItem = SourceDataStore.GetByPathAndId(serializedItemData.Path, serializedItemData.Id, serializedItemData.DatabaseName);

			// note that the evaluator is responsible for actual action being taken here
			// as well as logging what it does
			if (existingItem == null)
				existingItem = Evaluator.EvaluateNewSerializedItem(serializedItemData);
			else
				Evaluator.EvaluateUpdate(existingItem, serializedItemData);

			return new ItemLoadResult(ItemLoadStatus.Success, existingItem);
		}

		protected class ItemLoadResult
		{
			public ItemLoadResult(ItemLoadStatus status)
			{
				ItemData = null;
				Status = status;
			}

			public ItemLoadResult(ItemLoadStatus status, IItemData itemData)
			{
				ItemData = itemData;
				Status = status;
			}

			public IItemData ItemData { get; private set; }
			public ItemLoadStatus Status { get; private set; }
		}

		/// <summary>
		/// The result from loading a single item from disk
		/// </summary>
		protected enum ItemLoadStatus { Success, Skipped }

	}
}
