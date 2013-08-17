using Kamsar.WebConsole;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using System;
using System.Collections.Generic;
using System.Linq;
using Unicorn.Data;
using Unicorn.Predicates;
using Unicorn.Serialization;
using Unicorn.Evaluators;

namespace Unicorn
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

		public SerializationLoader(ISerializationProvider serializationProvider, ISourceDataProvider sourceDataProvider, IPredicate predicate, IEvaluator evaluator)
		{
			Assert.ArgumentNotNull(serializationProvider, "serializationProvider");
			Assert.ArgumentNotNull(sourceDataProvider, "sourceDataProvider");
			Assert.ArgumentNotNull(predicate, "predicate");
			Assert.ArgumentNotNull(evaluator, "evaluator");

			_evaluator = evaluator;
			_predicate = predicate;
			_serializationProvider = serializationProvider;
			_sourceDataProvider = sourceDataProvider;
		}

		/// <summary>
		/// Loads a preset from serialized items on disk.
		/// </summary>
		public void LoadTree(string database, string sitecorePath, IProgressStatus progress)
		{
			_itemsProcessed = 0;

			var rootSerializedItem = _serializationProvider.GetReference(sitecorePath, database);

			if (rootSerializedItem == null)
			{
				progress.ReportStatus("{0} was unable to find a root serialized item for {1}:{2}", MessageType.Error, _serializationProvider.GetType().Name, database, sitecorePath);
				return;
			}

			progress.ReportStatus("Loading serialized items under {0}:{1}", MessageType.Debug, database, sitecorePath);
			progress.ReportStatus("Provider root ID: " + rootSerializedItem.ProviderId, MessageType.Debug);

			using (new EventDisabler())
			{
				DoLoadTree(rootSerializedItem, progress);
			}

			DeserializationFinished(database);
		}

		/// <summary>
		/// Loads a specific path recursively, using any exclusions in the options' preset
		/// </summary>
		private void DoLoadTree(ISerializedReference root, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(progress, "progress");

			var failures = new List<Failure>();

			// go load the tree and see what failed, if anything
			LoadTreeRecursive(root, failures, progress);

			if (failures.Count > 0)
			{
				List<Failure> originalFailures;
				do
				{
					_sourceDataProvider.ResetTemplateEngine();

					// note tricky variable handling here, 'failures' used for two things
					originalFailures = failures;
					failures = new List<Failure>();

					foreach (var failure in originalFailures)
					{
						// retry loading a single item failure
						var item = failure.Reference as ISerializedItem;
						if (item != null)
						{
							try
							{
								DoLoadItem(item, progress);
							}
							catch (Exception reason)
							{
								failures.Add(new Failure(failure.Reference, reason));
							}

							continue;
						}

						// retry loading a reference failure (note the continues in the above ensure execution never arrives here for items)
						LoadTreeRecursive(failure.Reference, failures, progress);
					}
				}
				while (failures.Count > 0 && failures.Count < originalFailures.Count); // continue retrying until all possible failures have been fixed
			}

			if (failures.Count > 0)
			{
				foreach (var failure in failures)
				{
					progress.ReportStatus(string.Format("Failed to load {0} permanently because {1}", failure.Reference, failure.Reason), MessageType.Error);
				}

				throw new Exception("Some directories could not be loaded: " + failures[0].Reference, failures[0].Reason);
			}
		}

		/// <summary>
		/// Recursive method that loads a given tree and retries failures already present if any
		/// </summary>
		private void LoadTreeRecursive(ISerializedReference root, List<Failure> retryList, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(progress, "progress");
			Assert.ArgumentNotNull(retryList, "retryList");

			var included = _predicate.Includes(root);
			if (!included.IsIncluded)
			{
				// TODO: does this work?
				progress.ReportStatus("[S] {0}:{1} (and children) because it was excluded by {2}. However, it was present in {3}. {4}",	MessageType.Warning, root.DatabaseName, root.ItemPath, _predicate.GetType().Name, _serializationProvider.GetType().Name, included.Justification ?? string.Empty);
				return;
			}

			try
			{
				// load the current level
				LoadOneLevel(root, retryList, progress);

				// check if we have child paths to recurse down
				var children = _serializationProvider.GetChildReferences(root);

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
						LoadTreeRecursive(child, retryList, progress);
					}

					// pull out any standard values failures for immediate retrying
					List<Failure> standardValuesFailures = retryList.Where(x => x.Reason is StandardValuesException).ToList();
					retryList.RemoveAll(x => x.Reason is StandardValuesException);

					foreach (Failure current in standardValuesFailures)
					{
						try
						{
							var item = _serializationProvider.GetItem(current.Reference);
							DoLoadItem(item, progress);
						}
						catch (Exception reason)
						{
							retryList.Add(new Failure(current.Reference, reason));
						}
					}
				} // children.length > 0
			}
			catch (Exception ex)
			{
				retryList.Add(new Failure(root, ex));
			}
		}

		/// <summary>
		/// Loads a set of children from a serialized path
		/// </summary>
		private void LoadOneLevel(ISerializedReference root, List<Failure> retryList, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(progress, "progress");
			Assert.ArgumentNotNull(retryList, "retryList");

			var orphanCandidates = new Dictionary<ID, ISourceItem>();

			// grab the root item's full metadata
			var rootSerializedItem = _serializationProvider.GetItem(root);

			if (rootSerializedItem == null)
			{
				progress.ReportStatus("[S] {0}:{1}. Unable to get a serialized item for the path. <br />This usually indicates an orphaned serialized item tree in {2} which should be removed. <br />Less commonly, it could also indicate a sparsely serialized tree which is not supported.", MessageType.Warning, root.DatabaseName, root.ItemPath, _serializationProvider.GetType().Name);
				return;
			}

			// get the corresponding item from Sitecore
			ISourceItem rootItem = GetExistingItem(rootSerializedItem);

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
						progress.ReportStatus(string.Format("[S] {0}:{1} (and children) by {2}: {3}", child.Database, child.Path, _predicate.GetType().Name, included.Justification ?? string.Empty), MessageType.Debug);
					}
				}
			}

			// check for direct children of the target path
			var children = _serializationProvider.GetChildItems(rootSerializedItem);
			foreach (var child in children)
			{
				try
				{
					if (IsStandardValuesItem(child))
					{
						orphanCandidates.Remove(child.Id); // avoid deleting standard values items when forcing an update
						retryList.Add(new Failure(child, new StandardValuesException(child.ItemPath)));
					}
					else
					{
						// load a child item
						var loadedItem = DoLoadItem(child, progress);
						if (loadedItem.Item != null)
						{
							orphanCandidates.Remove(loadedItem.Item.Id);

							// check if we have any child serialized items under this loaded child item (existing children) -
							// if we do not, we can orphan any children of the loaded item as well
							var loadedItemsChildren = _serializationProvider.GetChildReferences(child);

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
					retryList.Add(new Failure(child, ex));
				}
			}

			// if we're forcing an update (ie deleting stuff not on disk) we send the items that we found that weren't on disk off to get evaluated by the evaluator
			if (orphanCandidates.Count > 0)
			{
				_evaluator.EvaluateOrphans(orphanCandidates.Values.ToArray(), progress);
			}
		}

		/// <summary>
		/// Loads a specific item from disk
		/// </summary>
		private ItemLoadResult DoLoadItem(ISerializedItem serializedItem, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(progress, "progress");

			bool disabledLocally = ItemHandler.DisabledLocally;
			try
			{
				ItemHandler.DisabledLocally = true;

				_itemsProcessed++;
				if (_itemsProcessed % 500 == 0 && _itemsProcessed > 1)
					progress.ReportStatus(string.Format("Processed {0} items", _itemsProcessed), MessageType.Debug);

				var included = _predicate.Includes(serializedItem);

				if (!included.IsIncluded)
				{
					progress.ReportStatus("[S] {0}:{1} by {2}; but it was in {3}. {4}<br />This usually indicates an extraneous excluded serialized item is present in the {3}, which should be removed.", MessageType.Warning, serializedItem.DatabaseName, serializedItem.ItemPath, _predicate.GetType().Name, _serializationProvider.GetType().Name, included.Justification ?? string.Empty);

					return new ItemLoadResult(ItemLoadStatus.Skipped);
				}

				// detect if we should run an update for the item or if it's already up to date
				var existingItem = GetExistingItem(serializedItem);
				if (existingItem == null || _evaluator.EvaluateUpdate(serializedItem, existingItem, progress))
				{
					string flag = (existingItem == null) ? "[A]" : "[U]";

					progress.ReportStatus("{0} {1}:{2}", MessageType.Info, flag, serializedItem.DatabaseName, serializedItem.ItemPath);

					ISourceItem updatedItem = _serializationProvider.DeserializeItem(serializedItem, progress);

					return new ItemLoadResult(ItemLoadStatus.Success, updatedItem);
				}

				return new ItemLoadResult(ItemLoadStatus.Success, existingItem);
			}
			finally
			{
				ItemHandler.DisabledLocally = disabledLocally;
			}
		}

		/// <summary>
		/// Raises the "serialization finished" event.
		/// </summary>
		private void DeserializationFinished(string databaseName)
		{
			EventManager.RaiseEvent(new SerializationFinishedEvent());
			Database database = Factory.GetDatabase(databaseName, false);
			if (database != null)
			{
				database.RemoteEvents.Queue.QueueEvent(new SerializationFinishedEvent());
			}
		}

		/// <summary>
		/// Determines whether a serialized item is a __Standard values item.
		/// </summary>
		/// <param name="item">The serialized item to test</param>
		/// <returns>
		///   <c>true</c> if [is standard values item] [the specified file name]; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsStandardValuesItem(ISerializedItem item)
		{
			Assert.ArgumentNotNull(item, "item");

			string[] array = item.ItemPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length > 0)
			{
				if (array.Any(s => s.Equals("templates", StringComparison.OrdinalIgnoreCase)))
				{
					return array.Last().Equals("__Standard Values", StringComparison.OrdinalIgnoreCase);
				}
			}

			return false;
		}

		protected ISourceItem GetExistingItem(ISerializedItem serializedItem)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");

			return _sourceDataProvider.GetItem(serializedItem.DatabaseName, serializedItem.Id);
		}

		private class StandardValuesException : Exception
		{
			public StandardValuesException(string itemPath)
				: base(itemPath)
			{
				Assert.ArgumentNotNull(itemPath, "itemPath");
			}

			public override string ToString()
			{
				return "Reverting of Standard values of template is delayed. " + Message;
			}
		}

		/// <summary>
		/// Represents a single failure in a recursive serialization load operation
		/// </summary>
		private class Failure
		{
			public ISerializedReference Reference { get; private set; }
			public Exception Reason { get; private set; }

			public Failure(ISerializedReference reference, Exception reason)
			{
				Assert.ArgumentNotNull(reference, "reference");
				Assert.ArgumentNotNull(reason, "reason");

				Reference = reference;
				Reason = reason;
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
