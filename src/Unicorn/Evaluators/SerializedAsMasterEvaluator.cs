using System.Linq;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Unicorn.ControlPanel;
using Unicorn.Data;
using Unicorn.Serialization;
using Registry = Unicorn.Dependencies.Registry;
using System.Collections.Generic;

namespace Unicorn.Evaluators
{
	public class SerializedAsMasterEvaluator : IEvaluator, IDocumentable
	{
		protected readonly ISerializedAsMasterEvaluatorLogger Logger;
		protected static readonly ID RootId = new ID("{11111111-1111-1111-1111-111111111111}");

		public SerializedAsMasterEvaluator(ISerializedAsMasterEvaluatorLogger logger = null)
		{
			logger = logger ?? Registry.Current.Resolve<ISerializedAsMasterEvaluatorLogger>();

			Assert.ArgumentNotNull(logger, "logger");

			Logger = logger;
		}

		public void EvaluateOrphans(ISourceItem[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");

			EvaluatorUtility.RecycleItems(orphanItems, item => Logger.DeletedItem(item));
		}

		public ISourceItem EvaluateNewSerializedItem(ISerializedItem newItem)
		{
			Assert.ArgumentNotNull(newItem, "newItem");

			Logger.SerializedNewItem(newItem);

			var updatedItem = DoDeserialization(newItem);

			return updatedItem;
		}

		public ISourceItem EvaluateUpdate(ISerializedItem serializedItem, ISourceItem existingItem)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(existingItem, "existingItem");

			var deferredUpdateLog = new DeferredLogWriter<ISerializedAsMasterEvaluatorLogger>();

			if (ShouldUpdateExisting(serializedItem, existingItem, deferredUpdateLog))
			{
				Logger.SerializedUpdatedItem(serializedItem);

				deferredUpdateLog.ExecuteDeferredActions(Logger);

				var updatedItem = DoDeserialization(serializedItem);

				return updatedItem;
			}

			return null;
		}

		protected virtual bool ShouldUpdateExisting(ISerializedItem serializedItem, ISourceItem existingItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(existingItem, "existingItem");

			if (existingItem.Id == RootId) return false; // we never want to update the Sitecore root item

			var orphanVersions = existingItem.Versions.Where(sourceItemVersion => serializedItem.GetVersion(sourceItemVersion.Language, sourceItemVersion.VersionNumber) == null).ToArray();
			if (orphanVersions.Length > 0)
			{
				deferredUpdateLog.AddEntry(x => x.OrphanSourceVersion(existingItem, serializedItem, orphanVersions));
				return true; // source contained versions not present in the serialized version, which is a difference
			}

			// see if the modified date is different in any version (because disk is master, ANY changes we want to force overwrite)
			return serializedItem.Versions.Any(serializedItemVersion =>
				{
					bool passedComparisons = false; // this flag lets us differentiate between items that we could not determine equality for, and items that just matched every criteria and don't need updating

					var sourceItemVersion = existingItem.GetVersion(serializedItemVersion.Language, serializedItemVersion.VersionNumber);

					// version exists in serialized item but does not in source version
					if (sourceItemVersion == null)
					{
						deferredUpdateLog.AddEntry(x => x.NewSerializedVersionMatch(serializedItemVersion, serializedItem, existingItem));
						return true;
					}

					var modifiedMatch = IsModifiedMatch(sourceItemVersion, serializedItemVersion, existingItem, serializedItem, deferredUpdateLog);
					if (modifiedMatch != null)
					{
						if (modifiedMatch.Value) return true;

						passedComparisons = true;
					}

					// ocasionally a version will not have a modified date or a modified date will not get updated, only a revision change so we compare those as a backup
					var revisionMatch = IsRevisionMatch(sourceItemVersion, serializedItemVersion, existingItem, serializedItem, deferredUpdateLog);

					if (revisionMatch != null)
					{
						if (revisionMatch.Value) return true;

						passedComparisons = true;
					}

					// as a last check, see if the names do not match. Renames, for example, only change the name and not the revision or modified date (wtf?)
					// unlike other checks, this one does not count as 'passing a comparison' if it fails to match a force. Having names be equal is not a strong enough measure of equality.
					if (IsNameMatch(existingItem, serializedItem, serializedItemVersion, deferredUpdateLog)) return true;

					// if we get here and no comparisons were passed, we have no valid updated or revision to compare. Let's ignore the item as if it was a real item it'd have one of these.
					if (!passedComparisons && !serializedItem.ItemPath.StartsWith("/sitecore/templates/System") && !serializedItem.ItemPath.StartsWith("/sitecore/templates/Sitecore Client")) // this occurs a lot in stock system templates - we ignore warnings for those as it's expected.
						deferredUpdateLog.AddEntry(x => x.CannotEvaluateUpdate(serializedItem, serializedItemVersion));

					return false;
				});
		}

		protected virtual bool? IsModifiedMatch(ItemVersion sourceItemVersion, ItemVersion serializedItemVersion, ISourceItem existingItem, ISerializedItem serializedItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			var serializedModified = serializedItemVersion.Updated;
			if (serializedModified == null) return null;

			var itemModified = sourceItemVersion.Updated;

			if (itemModified == null) return null;

			var result = !serializedModified.Value.Equals(itemModified.Value);

			if (result)
				deferredUpdateLog.AddEntry(x => x.IsModifiedMatch(serializedItem, serializedItemVersion, serializedModified.Value, itemModified.Value));

			return result;
		}

		protected virtual bool? IsRevisionMatch(ItemVersion sourceItemVersion, ItemVersion serializedItemVersion, ISourceItem existingItem, ISerializedItem serializedItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			var serializedRevision = serializedItemVersion.Revision;

			if (string.IsNullOrEmpty(serializedRevision)) return null;

			var result = !serializedRevision.Equals(sourceItemVersion.Revision);

			if (result)
				deferredUpdateLog.AddEntry(x => x.IsRevisionMatch(serializedItem, serializedItemVersion, serializedRevision, sourceItemVersion.Revision));

			return result;
		}

		protected virtual bool IsNameMatch(ISourceItem existingItem, ISerializedItem serializedItem, ItemVersion version, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			if (!serializedItem.Name.Equals(existingItem.Name))
			{
				deferredUpdateLog.AddEntry(x => x.IsNameMatch(serializedItem, existingItem, version));

				return true;
			}

			return false;
		}

		protected virtual ISourceItem DoDeserialization(ISerializedItem serializedItem)
		{
			ISourceItem updatedItem = serializedItem.Deserialize(false);

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
