using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Unicorn.Data;
using Unicorn.Dependencies;
using Unicorn.Predicates;
using Unicorn.Serialization;
using Unicorn.Evaluators;
using System.Diagnostics;

namespace Unicorn.Loader
{
	/// <summary>
	/// Custom loader that processes serialization loading with progress and additional rules options
	/// </summary>
	public class SerializationLoader
	{
		private int _itemsProcessed;
		protected readonly ISerializationProvider SerializationProvider;
		protected readonly IPredicate Predicate;
		protected readonly IEvaluator Evaluator;
		protected readonly ISourceDataProvider SourceDataProvider;
		protected readonly ISerializationLoaderLogger Logger;

		public SerializationLoader(ISerializationProvider serializationProvider = null, 
			ISourceDataProvider sourceDataProvider = null, 
			IPredicate predicate = null, 
			IEvaluator evaluator = null, 
			ISerializationLoaderLogger logger = null)
		{
			serializationProvider = serializationProvider ?? Registry.Current.Resolve<ISerializationProvider>();
			sourceDataProvider = sourceDataProvider ?? Registry.Current.Resolve<ISourceDataProvider>();
			predicate = predicate ?? Registry.Current.Resolve<IPredicate>();
			evaluator = evaluator ?? Registry.Current.Resolve<IEvaluator>();
			logger = logger ?? Registry.Current.Resolve<ISerializationLoaderLogger>();

			Assert.ArgumentNotNull(serializationProvider, "serializationProvider");
			Assert.ArgumentNotNull(sourceDataProvider, "sourceDataProvider");
			Assert.ArgumentNotNull(predicate, "predicate");
			Assert.ArgumentNotNull(evaluator, "evaluator");
			Assert.ArgumentNotNull(logger, "logger");

			Logger = logger;
			Evaluator = evaluator;
			Predicate = predicate;
			SerializationProvider = serializationProvider;
			SourceDataProvider = sourceDataProvider;
		}

		/// <summary>
		/// Loads all items in the configured predicate
		/// </summary>
		public virtual void LoadAll()
		{
			LoadAll(Registry.Current.Resolve<IDeserializeFailureRetryer>(), Registry.Current.Resolve<IConsistencyChecker>());
		}

		/// <summary>
		/// Loads all items in the configured predicate
		/// </summary>
		public virtual void LoadAll(IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(retryer, "retryer");

			var roots = Predicate.GetRootItems();
			foreach (var root in roots)
			{
				LoadTree(root, retryer, consistencyChecker);
			}
		}

		/// <summary>
		/// Loads a preset from serialized items on disk.
		/// </summary>
		public virtual void LoadTree(ISourceItem rootItem)
		{
			LoadTree(rootItem, Registry.Current.Resolve<IDeserializeFailureRetryer>(), Registry.Current.Resolve<IConsistencyChecker>());
		}

		/// <summary>
		/// Loads a preset from serialized items on disk.
		/// </summary>
		public virtual void LoadTree(ISourceItem rootItem, IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(rootItem, "rootItem");
			Assert.ArgumentNotNull(retryer, "retryer");
			Assert.ArgumentNotNull(consistencyChecker, "consistencyChecker");

			_itemsProcessed = 0;
			var timer = new Stopwatch();
			timer.Start();

			ISerializedReference rootSerializedReference = SerializationProvider.GetReference(rootItem);

			if (rootSerializedReference == null)
				throw new InvalidOperationException(string.Format("{0} was unable to find a root serialized reference for {1}", SerializationProvider.GetType().Name, rootItem.DisplayIdentifier));

			ISerializedItem rootSerializedItem = rootSerializedReference.GetItem();

			if (rootSerializedItem == null)
				throw new InvalidOperationException(string.Format("{0} was unable to find a root serialized item for {1}", SerializationProvider.GetType().Name, rootItem.DisplayIdentifier));

			Logger.BeginLoadingTree(rootSerializedItem);

			using (new EventDisabler())
			{
				// load the root item (LoadTreeRecursive only evaluates children)
				DoLoadItem(rootSerializedItem, consistencyChecker);

				// load children of the root
				LoadTreeRecursive(rootSerializedItem, retryer, consistencyChecker);

				retryer.RetryAll(SourceDataProvider, item => DoLoadItem(item, null), item => LoadTreeRecursive(item, retryer, null));
			}

			timer.Stop();

			SourceDataProvider.DeserializationComplete(rootItem.DatabaseName);
			Logger.EndLoadingTree(rootSerializedItem, _itemsProcessed, timer.ElapsedMilliseconds);
		}

		/// <summary>
		/// Recursive method that loads a given tree and retries failures already present if any
		/// </summary>
		protected virtual void LoadTreeRecursive(ISerializedReference root, IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(retryer, "retryer");

			var included = Predicate.Includes(root);
			if (!included.IsIncluded)
			{
				Logger.SkippedItemPresentInSerializationProvider(root, Predicate.GetType().Name, SerializationProvider.GetType().Name, included.Justification ?? string.Empty);
				return;
			}

			try
			{
				// load the current level
				LoadOneLevel(root, retryer, consistencyChecker);

				// check if we have child paths to recurse down
				var children = root.GetChildReferences(false);

				if (children.Length > 0)
				{
					// make sure if a "templates" item exists in the current set, it goes first
					if (children.Length > 1)
					{
						int templateIndex = Array.FindIndex(children, x => x.ItemPath.EndsWith("templates", StringComparison.OrdinalIgnoreCase));

						if (templateIndex > 0)
						{
							var zero = children[0];
							children[0] = children[templateIndex];
							children[templateIndex] = zero;
						}
					}

					// load each child path recursively
					foreach (var child in children)
					{
						LoadTreeRecursive(child, retryer, consistencyChecker);
					}

					// pull out any standard values failures for immediate retrying
					retryer.RetryStandardValuesFailures(item => DoLoadItem(item, null));
				} // children.length > 0
			}
			catch (ConsistencyException)
			{
				throw;
			}
			catch (Exception ex)
			{
				retryer.AddRetry(root, ex);
			}
		}

		/// <summary>
		/// Loads a set of children from a serialized path
		/// </summary>
		protected virtual void LoadOneLevel(ISerializedReference root, IDeserializeFailureRetryer retryer, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(retryer, "retryer");

			var orphanCandidates = new Dictionary<ID, ISourceItem>();

			// grab the root item's full metadata
			var rootSerializedItem = root.GetItem();

			if (rootSerializedItem == null)
			{
				Logger.SkippedItemMissingInSerializationProvider(root, SerializationProvider.GetType().Name);
				return;
			}

			// get the corresponding item from Sitecore
			ISourceItem rootItem = SourceDataProvider.GetItemById(rootSerializedItem.DatabaseName, rootSerializedItem.Id);

			// we add all of the root item's direct children to the "maybe orphan" list (we'll remove them as we find matching serialized children)
			if (rootItem != null)
			{
				var rootChildren = rootItem.Children;
				foreach (ISourceItem child in rootChildren)
				{
					// if the preset includes the child add it to the orphan-candidate list (if we don't deserialize it below, it will be marked orphan)
					var included = Predicate.Includes(child);
					if (included.IsIncluded)
						orphanCandidates[child.Id] = child;
					else
					{
						Logger.SkippedItem(child, Predicate.GetType().Name, included.Justification ?? string.Empty);
					}
				}
			}

			// check for direct children of the target path
			var children =rootSerializedItem.GetChildItems();
			foreach (var child in children)
			{
				try
				{
					if (child.IsStandardValuesItem)
					{
						orphanCandidates.Remove(child.Id); // avoid marking standard values items orphans
						retryer.AddRetry(child, new StandardValuesException(child.ItemPath));
					}
					else
					{
						// load a child item
						var loadedItem = DoLoadItem(child, consistencyChecker);
						if (loadedItem.Item != null)
						{
							orphanCandidates.Remove(loadedItem.Item.Id);

							// check if we have any child serialized items under this loaded child item (existing children) -
							// if we do not, we can orphan any children of the loaded item as well
							var loadedItemsChildren = child.GetChildReferences(false);

							if (loadedItemsChildren.Length == 0) // no children were serialized on disk
							{
								var loadedChildren = loadedItem.Item.Children;
								foreach (ISourceItem loadedChild in loadedChildren)
								{
									orphanCandidates.Add(loadedChild.Id, loadedChild);
								}
							}
						}
						else if (loadedItem.Status == ItemLoadStatus.Skipped) // if the item got skipped we'll prevent it from being deleted
							orphanCandidates.Remove(child.Id);
					}
				}
				catch (ConsistencyException)
				{
					throw;
				}
				catch (Exception ex)
				{
					// if a problem occurs we attempt to retry later
					retryer.AddRetry(child, ex);

					// don't treat errors as cause to delete an item
					orphanCandidates.Remove(child.Id);
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
		protected virtual ItemLoadResult DoLoadItem(ISerializedItem serializedItem, IConsistencyChecker consistencyChecker)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");

			if (consistencyChecker != null)
			{
				if (!consistencyChecker.IsConsistent(serializedItem)) throw new ConsistencyException("Consistency check failed - aborting loading.");
				consistencyChecker.AddProcessedItem(serializedItem);
			}

			bool disableNewSerialization = UnicornDataProvider.DisableSerialization;
			try
			{
				UnicornDataProvider.DisableSerialization = true;

				_itemsProcessed++;

				var included = Predicate.Includes(serializedItem);

				if (!included.IsIncluded)
				{
					Logger.SkippedItemPresentInSerializationProvider(serializedItem, Predicate.GetType().Name, SerializationProvider.GetType().Name, included.Justification ?? string.Empty);
					return new ItemLoadResult(ItemLoadStatus.Skipped);
				}

				// detect if we should run an update for the item or if it's already up to date
				var existingItem = SourceDataProvider.GetItemById(serializedItem.DatabaseName, serializedItem.Id);
				ISourceItem updatedItem;

				// note that the evaluator is responsible for actual action being taken here
				// as well as logging what it does
				if (existingItem == null)
					updatedItem = Evaluator.EvaluateNewSerializedItem(serializedItem);
				else
					updatedItem = Evaluator.EvaluateUpdate(serializedItem, existingItem);
				
				return new ItemLoadResult(ItemLoadStatus.Success, updatedItem ?? existingItem);
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
				Item = null;
				Status = status;
			}

			public ItemLoadResult(ItemLoadStatus status, ISourceItem item)
			{
				Item = item;
				Status = status;
			}

			public ISourceItem Item { get; private set; }
			public ItemLoadStatus Status { get; private set; }
		}

		/// <summary>
		/// The result from loading a single item from disk
		/// </summary>
		protected enum ItemLoadStatus { Success, Skipped }

	}
}
