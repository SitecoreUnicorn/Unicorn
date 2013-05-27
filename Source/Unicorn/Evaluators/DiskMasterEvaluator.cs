using System;
using Kamsar.WebConsole;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public class DiskMasterEvaluator : IEvaluator
	{
		public void EvaluateOrphans(Item[] orphanItems, IProgressStatus progress)
		{
			EvaluatorUtility.DeleteItems(orphanItems, progress, (innerProgress, item) => innerProgress.ReportStatus("[DELETING] {0}:{1} because it did not exist on disk".FormatWith(item.Database.Name, item.Paths.FullPath), MessageType.Warning));
		}

		public bool EvaluateUpdate(ISerializedItem serializedItem, Item existingItem)
		{
			/*
			 Database db = Factory.GetDatabase(syncItem.DatabaseName);

			Assert.IsNotNull(db, "Database was null");

			Item target = db.GetItem(syncItem.ID);

			if (target == null) return true; // target item doesn't exist - must force

			// see if the modified date is different in any version (because disk is master, ANY changes we want to force overwrite)
			return syncItem.Versions.Any(version =>
				{
					Item targetVersion = target.Database.GetItem(target.ID, Language.Parse(version.Language), Sitecore.Data.Version.Parse(version.Version));
					var serializedModified = version.Values[FieldIDs.Updated.ToString()];
					var itemModified = targetVersion[FieldIDs.Updated.ToString()];

					if (!string.IsNullOrEmpty(serializedModified))
					{
						var result = string.Compare(serializedModified, itemModified, StringComparison.InvariantCulture) != 0;


						if (result)
							progress.ReportStatus(
								string.Format("{0} ({1} #{2}): Disk modified {3}, Item modified {4}", syncItem.ItemPath, version.Language,
											version.Version, serializedModified, itemModified), MessageType.Debug);

						return result;
					}

					// ocasionally a version will not have a modified date, only a revision so we compare those as a backup
					var serializedRevision = version.Values[FieldIDs.Revision.ToString()];
					var itemRevision = targetVersion.Statistics.Revision;

					if (!string.IsNullOrEmpty(serializedRevision))
					{
						var result = string.Compare(serializedRevision, itemRevision, StringComparison.InvariantCulture) != 0;

						if (result)
							progress.ReportStatus(
								string.Format("{0} ({1} #{2}): Disk revision {3}, Item revision {4}", syncItem.ItemPath, version.Language,
											version.Version, serializedRevision, itemRevision), MessageType.Debug);

						return result;
					}

					// if we get here we have no valid updated or revision to compare. Let's ignore the item as if it was a real item it'd have one of these.
					if (!syncItem.ItemPath.StartsWith("/sitecore/templates/System") && !syncItem.ItemPath.StartsWith("/sitecore/templates/Sitecore Client")) // this occurs a lot in stock system templates - we ignore warnings for those as it's expected.
						progress.ReportStatus(string.Format("{0} ({1} #{2}): Serialized version had no modified or revision field to check for update.", syncItem.ItemPath, version.Language, version.Version), MessageType.Warning);

					return false;
				}); */

			throw new Exception();
		}
	}
}
