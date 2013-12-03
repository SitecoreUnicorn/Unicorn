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

			// see if the modified date is different in any version (because disk is master, ANY changes we want to force overwrite)
			return serializedItem.Versions.Any(version =>
				{
					bool passedComparisons = false; // this flag lets us differentiate between items that we could not determine equality for, and items that just matched every criteria and don't need updating

					var modifiedMatch = IsModifiedMatch(existingItem, serializedItem, version);
					if (modifiedMatch != null)
					{
						if (modifiedMatch.Value) return true;

						passedComparisons = true;
					}

					// ocasionally a version will not have a modified date or a modified date will not get updated, only a revision change so we compare those as a backup
					var revisionMatch = IsRevisionMatch(existingItem, serializedItem, version);

					if (revisionMatch != null)
					{
						if (revisionMatch.Value) return true;

						passedComparisons = true;
					}

					// as a last check, see if the names do not match. Renames, for example, only change the name and not the revision or modified date (wtf?)
					// unlike other checks, this one does not count as 'passing a comparison' if it fails to match a force. Having names be equal is not a strong enough measure of equality.
					if (IsNameMatch(existingItem, serializedItem, version)) return true;

					// if we get here and no comparisons were passed, we have no valid updated or revision to compare. Let's ignore the item as if it was a real item it'd have one of these.
					if (!passedComparisons && !serializedItem.ItemPath.StartsWith("/sitecore/templates/System") && !serializedItem.ItemPath.StartsWith("/sitecore/templates/Sitecore Client")) // this occurs a lot in stock system templates - we ignore warnings for those as it's expected.
						_logger.CannotEvaluateUpdate(serializedItem, version);

					return false;
				});
		}

		protected virtual bool? IsModifiedMatch(ISourceItem existingItem, ISerializedItem serializedItem, SerializedVersion version)
		{
			var serializedModified = version.Updated;
			if (serializedModified == null) return null;

			var itemModified = existingItem.GetLastModifiedDate(version.Language, version.VersionNumber);

			if (itemModified == null) return null;

			var result = !serializedModified.Value.Equals(itemModified.Value);

			if (result)
				_logger.IsModifiedMatch(serializedItem, version, serializedModified.Value, itemModified.Value);

			return result;
		}

		protected virtual bool? IsRevisionMatch(ISourceItem existingItem, ISerializedItem serializedItem, SerializedVersion version)
		{
			var serializedRevision = version.Revision;

			if (string.IsNullOrEmpty(serializedRevision)) return null;

			var itemRevision = existingItem.GetRevision(version.Language, version.VersionNumber);
			var result = !serializedRevision.Equals(itemRevision);

			if (result)
				_logger.IsRevisionMatch(serializedItem, version, serializedRevision, itemRevision);

			return result;
		}

		protected virtual bool IsNameMatch(ISourceItem existingItem, ISerializedItem serializedItem, SerializedVersion version)
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
