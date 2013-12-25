using System.Linq;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Unicorn.Data;
using Unicorn.Dependencies;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public class SerializedAsMasterEvaluator : IEvaluator
	{
		private readonly ISerializedAsMasterEvaluatorLogger _logger;
		private static readonly ID RootId = new ID("{11111111-1111-1111-1111-111111111111}");

		public SerializedAsMasterEvaluator(ISerializedAsMasterEvaluatorLogger logger = null)
		{
			logger = logger ?? Registry.Current.Resolve<ISerializedAsMasterEvaluatorLogger>();

			Assert.ArgumentNotNull(logger, "logger");

			_logger = logger;
		}

		public void EvaluateOrphans(ISourceItem[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");

			EvaluatorUtility.RecycleItems(orphanItems, item => _logger.DeletedItem(item));
		}

		public bool EvaluateUpdate(ISerializedItem serializedItem, ISourceItem existingItem)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");

			if (existingItem == null) return true;

			if (existingItem.Id == RootId) return false; // we never want to update the Sitecore root item

			bool newVersions = existingItem.Versions.Any(sourceItemVersion => serializedItem.GetVersion(sourceItemVersion.Language, sourceItemVersion.VersionNumber) == null);
			if (newVersions) return true; // source contained versions not present in the serialized version, which is a difference

			// see if the modified date is different in any version (because disk is master, ANY changes we want to force overwrite)
			return serializedItem.Versions.Any(serializedItemVersion =>
				{
					bool passedComparisons = false; // this flag lets us differentiate between items that we could not determine equality for, and items that just matched every criteria and don't need updating

					var sourceItemVersion = existingItem.GetVersion(serializedItemVersion.Language, serializedItemVersion.VersionNumber);

					// version exists in serialized item but does not in source version
					if (sourceItemVersion == null)
					{
						_logger.NewSerializedVersionMatch(serializedItemVersion, serializedItem, existingItem);
						return true;
					}

					var modifiedMatch = IsModifiedMatch(sourceItemVersion, serializedItemVersion, existingItem, serializedItem);
					if (modifiedMatch != null)
					{
						if (modifiedMatch.Value) return true;

						passedComparisons = true;
					}

					// ocasionally a version will not have a modified date or a modified date will not get updated, only a revision change so we compare those as a backup
					var revisionMatch = IsRevisionMatch(sourceItemVersion, serializedItemVersion, existingItem, serializedItem);

					if (revisionMatch != null)
					{
						if (revisionMatch.Value) return true;

						passedComparisons = true;
					}

					// as a last check, see if the names do not match. Renames, for example, only change the name and not the revision or modified date (wtf?)
					// unlike other checks, this one does not count as 'passing a comparison' if it fails to match a force. Having names be equal is not a strong enough measure of equality.
					if (IsNameMatch(existingItem, serializedItem, serializedItemVersion)) return true;

					// if we get here and no comparisons were passed, we have no valid updated or revision to compare. Let's ignore the item as if it was a real item it'd have one of these.
					if (!passedComparisons && !serializedItem.ItemPath.StartsWith("/sitecore/templates/System") && !serializedItem.ItemPath.StartsWith("/sitecore/templates/Sitecore Client")) // this occurs a lot in stock system templates - we ignore warnings for those as it's expected.
						_logger.CannotEvaluateUpdate(serializedItem, serializedItemVersion);

					return false;
				});
		}

		protected virtual bool? IsModifiedMatch(ItemVersion sourceItemVersion, ItemVersion serializedItemVersion, ISourceItem existingItem, ISerializedItem serializedItem)
		{
			var serializedModified = serializedItemVersion.Updated;
			if (serializedModified == null) return null;

			var itemModified = sourceItemVersion.Updated;

			if (itemModified == null) return null;

			var result = !serializedModified.Value.Equals(itemModified.Value);

			if (result)
				_logger.IsModifiedMatch(serializedItem, serializedItemVersion, serializedModified.Value, itemModified.Value);

			return result;
		}

		protected virtual bool? IsRevisionMatch(ItemVersion sourceItemVersion, ItemVersion serializedItemVersion, ISourceItem existingItem, ISerializedItem serializedItem)
		{
			var serializedRevision = serializedItemVersion.Revision;

			if (string.IsNullOrEmpty(serializedRevision)) return null;

			var result = !serializedRevision.Equals(sourceItemVersion.Revision);

			if (result)
				_logger.IsRevisionMatch(serializedItem, serializedItemVersion, serializedRevision, sourceItemVersion.Revision);

			return result;
		}

		protected virtual bool IsNameMatch(ISourceItem existingItem, ISerializedItem serializedItem, ItemVersion version)
		{
			if (!serializedItem.Name.Equals(existingItem.Name))
			{
				_logger.IsNameMatch(serializedItem, existingItem, version);
				
				return true;
			}

			return false;
		}
	}
}
