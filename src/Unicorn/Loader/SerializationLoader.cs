using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Unicorn.Data;
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
		private readonly ISerializationProvider _serializationProvider;
		private readonly IPredicate _predicate;
		private readonly IEvaluator _evaluator;
		private readonly ISourceDataProvider _sourceDataProvider;
		private readonly ISerializationLoaderLogger _logger;

		public SerializationLoader(ISerializationProvider serializationProvider, ISourceDataProvider sourceDataProvider, IPredicate predicate, IEvaluator evaluator, ISerializationLoaderLogger logger)
		{
			_logger = logger;
			Assert.ArgumentNotNull(serializationProvider, "serializationProvider");
			Assert.ArgumentNotNull(sourceDataProvider, "sourceDataProvider");
			Assert.ArgumentNotNull(predicate, "predicate");
			Assert.ArgumentNotNull(evaluator, "evaluator");
			Assert.ArgumentNotNull(logger, "logger");

			_evaluator = evaluator;
			_predicate = predicate;
			_serializationProvider = serializationProvider;
			_sourceDataProvider = sourceDataProvider;
		}

		/// <summary>
		/// Loads a preset from serialized items on disk.
		/// </summary>
		public void LoadTree(ISourceItem rootItem)
		{
			LoadTree(rootItem, new DeserializeFailureRetryer());
		}

		/// <summary>
		/// Loads a preset from serialized items on disk.
		/// </summary>
		public void LoadTree(ISourceItem rootItem, IDeserializeFailureRetryer retryer)
		{
			Assert.ArgumentNotNull(rootItem, "rootItem");
			Assert.ArgumentNotNull(retryer, "retryer");

			_itemsProcessed = 0;
			var timer = new Stopwatch();
			timer.Start();

			ISerializedItem rootSerializedItem = _serializationProvider.GetItem(_serializationProvider.GetReference(rootItem));

			if (rootSerializedItem == null)
				throw new InvalidOperationException(string.Format("{0} was unable to find a root serialized item for {1}", _serializationProvider.GetType().Name, rootItem.DisplayIdentifier));

			_logger.BeginLoadingTree(rootSerializedItem);

			using (new EventDisabler())
			{
				// load the root item (LoadTreeRecursive only evaluates children)
				DoLoadItem(rootSerializedItem);

				// load children of the root
				LoadTreeRecursive(rootSerializedItem, retryer);
			}

			timer.Stop();

			_sourceDataProvider.DeserializationComplete(rootItem.DatabaseName);
			_logger.EndLoadingTree(rootSerializedItem, _itemsProcessed, timer.ElapsedMilliseconds);
		}

		/// <summary>
		/// Recursive method that loads a given tree and retries failures already present if any
		/// </summary>
		private void LoadTreeRecursive(ISerializedReference root, IDeserializeFailureRetryer retryer)
		{
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(retryer, "retryer");

			var included = _predicate.Includes(root);
			if (!included.IsIncluded)
			{
				_logger.SkippedItemPresentInSerializationProvider(root, _predicate.GetType().Name, _serializationProvider.GetType().Name, included.Justification ?? string.Empty);
				return;
			}

			try
			{
				// load the current level
				LoadOneLevel(root, retryer);

				// check if we have child paths to recurse down
				var children = _serializationProvider.GetChildReferences(root, false);

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
						LoadTreeRecursive(child, retryer);
					}

					// pull out any standard values failures for immediate retrying
					retryer.RetryStandardValuesFailures(item => DoLoadItem(item));
				} // children.length > 0
			}
			catch (Exception ex)
			{
				retryer.AddRetry(root, ex);
			}
		}

		/// <summary>
		/// Loads a set of children from a serialized path
		/// </summary>
		private void LoadOneLevel(ISerializedReference root, IDeserializeFailureRetryer retryer)
		{
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(retryer, "retryer");

			var orphanCandidates = new Dictionary<ID, ISourceItem>();

			// grab the root item's full metadata
			var rootSerializedItem = _serializationProvider.GetItem(root);

			if (rootSerializedItem == null)
			{
				_logger.SkippedItemMissingInSerializationProvider(root, _serializationProvider.GetType().Name);
				return;
			}

			// get the corresponding item from Sitecore
			ISourceItem rootItem = _sourceDataProvider.GetItem(rootSerializedItem.DatabaseName, rootSerializedItem.Id);

			// we add all of the root item's direct children to the "maybe orphan" list (we'll remove them as we find matching serialized children)
			if (rootItem != null)
			{
				foreach (ISourceItem child in rootItem.Children)
				{
					// if the preset includes the child add it to the orphan-candidate list (if we don't deserialize it below, it will be marked orphan)
					var included = _predicate.Includes(child);
					if (included.IsIncluded)
						orphanCandidates[child.Id] = child;
					else
					{
						_logger.SkippedItem(child, _predicate.GetType().Name, included.Justification ?? string.Empty);
					}
				}
			}

			// check for direct children of the target path
			var children = _serializationProvider.GetChildItems(rootSerializedItem);
			foreach (var child in children)
			{
				try
				{
					if (_serializationProvider.IsStandardValuesItem(child))
					{
						orphanCandidates.Remove(child.Id); // avoid marking standard values items orphans
						retryer.AddRetry(child, new StandardValuesException(child.ItemPath));
					}
					else
					{
						// load a child item
						var loadedItem = DoLoadItem(child);
						if (loadedItem.Item != null)
						{
							orphanCandidates.Remove(loadedItem.Item.Id);

							// check if we have any child serialized items under this loaded child item (existing children) -
							// if we do not, we can orphan any children of the loaded item as well
							var loadedItemsChildren = _serializationProvider.GetChildReferences(child, false);

							if (loadedItemsChildren.Length == 0) // no children were serialized on disk
							{
								foreach (ISourceItem loadedChild in loadedItem.Item.Children)
								{
									orphanCandidates.Add(loadedChild.Id, loadedChild);
								}
							}
						}
						else if (loadedItem.Status == ItemLoadStatus.Skipped) // if the item got skipped we'll prevent it from being deleted
							orphanCandidates.Remove(child.Id);
					}
				}
				catch (Exception ex)
				{
					// if a problem occurs we attempt to retry later
					retryer.AddRetry(child, ex);
				}
			}

			// if we're forcing an update (ie deleting stuff not on disk) we send the items that we found that weren't on disk off to get evaluated as orphans
			if (orphanCandidates.Count > 0)
			{
				_evaluator.EvaluateOrphans(orphanCandidates.Values.ToArray());
			}
		}

		/// <summary>
		/// Loads a specific item from disk
		/// </summary>
		private ItemLoadResult DoLoadItem(ISerializedItem serializedItem)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");

			bool disableNewSerialization = UnicornDataProvider.DisableSerialization;
			try
			{
				UnicornDataProvider.DisableSerialization = true;

				_itemsProcessed++;

				var included = _predicate.Includes(serializedItem);

				if (!included.IsIncluded)
				{
					_logger.SkippedItemPresentInSerializationProvider(serializedItem, _predicate.GetType().Name, _serializationProvider.GetType().Name, included.Justification ?? string.Empty);
					return new ItemLoadResult(ItemLoadStatus.Skipped);
				}

				// detect if we should run an update for the item or if it's already up to date
				var existingItem = _sourceDataProvider.GetItem(serializedItem.DatabaseName, serializedItem.Id);
				if (existingItem == null || _evaluator.EvaluateUpdate(serializedItem, existingItem))
				{
					ISourceItem updatedItem = _serializationProvider.DeserializeItem(serializedItem);

					Assert.IsNotNull(updatedItem, "Do not return null from DeserializeItem() - throw an exception if an error occurs.");

					if (existingItem == null)
						_logger.SerializedNewItem(serializedItem);
					else
						_logger.SerializedUpdatedItem(serializedItem);	
						
					return new ItemLoadResult(ItemLoadStatus.Success, updatedItem);
				}

				return new ItemLoadResult(ItemLoadStatus.Success, existingItem);
			}
			finally
			{
				UnicornDataProvider.DisableSerialization = disableNewSerialization;
			}
		}

		private class ItemLoadResult
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
		private enum ItemLoadStatus { Success, Skipped }

	}
}
