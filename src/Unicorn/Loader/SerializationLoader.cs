using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Data.Events;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Evaluators;
using Unicorn.Predicates;

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
		protected readonly PredicateRootPathResolver PredicateRootPathResolver;

		private int _threads = Settings.GetIntSetting("Unicorn.MaximumConcurrency", 16);

		public int ThreadCount
		{
			get { return _threads; }
			set { _threads = value; }
		}

		public SerializationLoader(ITargetDataStore targetDataStore, ISourceDataStore sourceDataStore, IPredicate predicate, IEvaluator evaluator, ISerializationLoaderLogger logger, PredicateRootPathResolver predicateRootPathResolver)
		{
			Assert.ArgumentNotNull(targetDataStore, "serializationProvider");
			Assert.ArgumentNotNull(sourceDataStore, "sourceDataStore");
			Assert.ArgumentNotNull(predicate, "predicate");
			Assert.ArgumentNotNull(evaluator, "evaluator");
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(predicateRootPathResolver, "predicateRootPathResolver");

			Logger = logger;
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

			using (new EventDisabler())
			{
				foreach (var rootItem in rootItemsData)
				{
					LoadTree(rootItem, retryer, consistencyChecker);
					if (rootLoadedCallback != null) rootLoadedCallback(rootItem);
				}
			}
			
			retryer.RetryAll(SourceDataStore, item => DoLoadItem(item, null), item => LoadTreeInternal(item, retryer, null));
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


			// load the root item (LoadTreeRecursive only evaluates children)
			DoLoadItem(rootItemData, consistencyChecker);

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
				Logger.SkippedItemPresentInSerializationProvider(root, Predicate.FriendlyName, TargetDataStore.GetType().Name, included.Justification ?? string.Empty);
				return;
			}

			// we throw items into this queue, and let a thread pool pick up anything available to process in parallel. only the children of queued items are processed, not the item itself
			ConcurrentQueue<IItemData> processQueue = new ConcurrentQueue<IItemData>();

			// exceptions thrown on background threads are left in here
			ConcurrentQueue<Exception> errors = new ConcurrentQueue<Exception>();

			// we keep track of how many threads are actively processing something so we know when to end the threads
			// (e.g. a thread could have nothing in the queue right now, but that's because a different thread is about
			// to add 8 things to the queue - so it shouldn't quit till all is done)
			int activeThreads = 0;

			// put the root in the queue
			processQueue.Enqueue(root);

			Thread[] pool = Enumerable.Range(0, ThreadCount).Select(i => new Thread(() =>
			{
				Process:
				Interlocked.Increment(ref activeThreads);
				IItemData parentItem;
				

				while (processQueue.TryDequeue(out parentItem) && errors.Count == 0)
				{
					try
					{
						using (new SecurityDisabler())
						{
							// load the current level
							LoadOneLevel(parentItem, retryer, consistencyChecker);

							// check if we have child paths to process down
							var children = TargetDataStore.GetChildren(parentItem).ToArray();

							if (children.Length > 0)
							{
								// make sure if a "templates" item exists in the current set, it goes first
								if (children.Length > 1)
								{
									int templateIndex = Array.FindIndex(children,
										x => x.Path.EndsWith("templates", StringComparison.OrdinalIgnoreCase));

									if (templateIndex > 0)
									{
										var zero = children[0];
										children[0] = children[templateIndex];
										children[templateIndex] = zero;
									}
								}

								// load each child path
								foreach (var child in children)
								{
									processQueue.Enqueue(child);
								}

								// pull out any standard values failures for immediate retrying
								retryer.RetryStandardValuesFailures(item => DoLoadItem(item, null));
							} // children.length > 0
						}
					}
					catch (ConsistencyException cex)
					{
						errors.Enqueue(cex);
						break;
					}
					catch (Exception ex)
					{
						retryer.AddTreeRetry(root, ex);
					}
				}

				// if we get here, the queue was empty. let's make ourselves inactive.
				Interlocked.Decrement(ref activeThreads);

				// if some other thread in our pool was doing stuff, sleep for a sec to see if we can pick up their work
				if (activeThreads > 0)
				{
					Thread.Sleep(10);
					goto Process; // OH MY GOD :)
				}
			})).ToArray();

			// start the thread pool
			foreach (var thread in pool) thread.Start();

			// ...and then wait for all the threads to finish
			foreach (var thread in pool) thread.Join();

			if(errors.Count > 0) throw new AggregateException(errors);
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
					if (serializedChild.IsStandardValuesItem())
					{
						orphanCandidates.Remove(serializedChild.Id); // avoid marking standard values items orphans
						retryer.AddItemRetry(serializedChild, new StandardValuesException(serializedChild.Path));
					}
					else
					{
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
									if(Predicate.Includes(loadedSourceChild).IsIncluded)
										orphanCandidates.Add(loadedSourceChild.Id, loadedSourceChild);
								}
							}
						}
						else if (loadedSourceItem.Status == ItemLoadStatus.Skipped) // if the item got skipped we'll prevent it from being deleted
							orphanCandidates.Remove(serializedChild.Id);
					}
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
				bool disableNewSerialization = UnicornDataProvider.DisableSerialization;
				try
				{
					UnicornDataProvider.DisableSerialization = true;
					Evaluator.EvaluateOrphans(orphanCandidates.Values.ToArray());
				}
				finally
				{
					UnicornDataProvider.DisableSerialization = disableNewSerialization;
				}
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

			bool disableNewSerialization = UnicornDataProvider.DisableSerialization;
			try
			{
				UnicornDataProvider.DisableSerialization = true;

				_itemsProcessed++;

				var included = Predicate.Includes(serializedItemData);

				if (!included.IsIncluded)
				{
					Logger.SkippedItemPresentInSerializationProvider(serializedItemData, Predicate.FriendlyName, TargetDataStore.FriendlyName, included.Justification ?? string.Empty);
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
			finally
			{
				UnicornDataProvider.DisableSerialization = disableNewSerialization;
			}
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
