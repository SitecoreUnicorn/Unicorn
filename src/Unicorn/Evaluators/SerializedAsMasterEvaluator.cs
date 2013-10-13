using System.Linq;
using Kamsar.WebConsole;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public class SerializedAsMasterEvaluator : IEvaluator
	{
		public void EvaluateOrphans(ISourceItem[] orphanItems, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");
			Assert.ArgumentNotNull(progress, "progress");

			EvaluatorUtility.RecycleItems(orphanItems, progress, (innerProgress, item) => innerProgress.ReportStatus("[D] {0} because it did not exist in the serialization provider.".FormatWith(item.DisplayIdentifier), MessageType.Warning));
		}

		public bool EvaluateUpdate(ISerializedItem serializedItem, ISourceItem existingItem, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(progress, "progress");

			if (existingItem == null) return true;

			// see if the modified date is different in any version (because disk is master, ANY changes we want to force overwrite)
			return serializedItem.Versions.Any(version =>
				{
					bool passedComparisons = false; // this flag lets us differentiate between items that we could not determine equality for, and items that just matched every criteria and don't need updating

					var modifiedMatch = IsModifiedMatch(existingItem, serializedItem, version, progress);
					if (modifiedMatch != null)
					{
						if (modifiedMatch.Value) return true;

						passedComparisons = true;
					}

					// ocasionally a version will not have a modified date or a modified date will not get updated, only a revision change so we compare those as a backup
					var revisionMatch = IsRevisionMatch(existingItem, serializedItem, version, progress);

					if (revisionMatch != null)
					{
						if (revisionMatch.Value) return true;

						passedComparisons = true;
					}

					// as a last check, see if the names do not match. Renames, for example, only change the name and not the revision or modified date (wtf?)
					// unlike other checks, this one does not count as 'passing a comparison' if it fails to match a force. Having names be equal is not a strong enough measure of equality.
					if (IsNameMatch(existingItem, serializedItem, version, progress)) return true;

					// if we get here and no comparisons were passed, we have no valid updated or revision to compare. Let's ignore the item as if it was a real item it'd have one of these.
					if (!passedComparisons && !serializedItem.ItemPath.StartsWith("/sitecore/templates/System") && !serializedItem.ItemPath.StartsWith("/sitecore/templates/Sitecore Client")) // this occurs a lot in stock system templates - we ignore warnings for those as it's expected.
						progress.ReportStatus(string.Format("{0} ({1} #{2}): Serialized version had no modified or revision field to check for update.", serializedItem.ItemPath, version.Language, version.VersionNumber), MessageType.Warning);

					return false;
				});
		}

		protected virtual bool? IsModifiedMatch(ISourceItem existingItem, ISerializedItem serializedItem, SerializedVersion version, IProgressStatus progress)
		{
			var serializedModified = version.Updated;
			if (serializedModified == null) return null;

			var itemModified = existingItem.GetLastModifiedDate(version.Language, version.VersionNumber);

			if (itemModified == null) return null;

			var result = !serializedModified.Value.Equals(itemModified.Value);

			if (result)
				progress.ReportStatus(string.Format("{0} ({1} #{2}): Disk modified {3}, Item modified {4}", serializedItem.ItemPath, version.Language, version.VersionNumber, serializedModified.Value.ToString("G"), itemModified.Value.ToString("G")), MessageType.Debug);

			return result;
		}

		protected virtual bool? IsRevisionMatch(ISourceItem existingItem, ISerializedItem serializedItem, SerializedVersion version, IProgressStatus progress)
		{
			var serializedRevision = version.Revision;

			if (string.IsNullOrEmpty(serializedRevision)) return null;

			var itemRevision = existingItem.GetRevision(version.Language, version.VersionNumber);
			var result = !serializedRevision.Equals(itemRevision);

			if (result)
				progress.ReportStatus(string.Format("{0} ({1} #{2}): Disk revision {3}, Item revision {4}", serializedItem.ItemPath, version.Language, version.VersionNumber, serializedRevision, itemRevision), MessageType.Debug);

			return result;
		}

		protected virtual bool IsNameMatch(ISourceItem existingItem, ISerializedItem serializedItem, SerializedVersion version, IProgressStatus progress)
		{
			if (!serializedItem.Name.Equals(existingItem.Name))
			{
				progress.ReportStatus(string.Format("{0} ({1} #{2}): Disk name {3}, Item name {4}", serializedItem.ItemPath, version.Language, version.VersionNumber, serializedItem.Name, existingItem.Name), MessageType.Debug);
				return true;
			}

			return false;
		}
	}
}
