using System;
using System.Linq;
using Kamsar.WebConsole;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.StringExtensions;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public class SerializedAsMasterEvaluator : IEvaluator
	{
		public void EvaluateOrphans(Item[] orphanItems, IProgressStatus progress)
		{
			EvaluatorUtility.DeleteItems(orphanItems, progress, (innerProgress, item) => innerProgress.ReportStatus("[D] {0}:{1} because it did not exist in the serialization provider.".FormatWith(item.Database.Name, item.Paths.FullPath), MessageType.Warning));
		}

		public bool EvaluateUpdate(ISerializedItem serializedItem, Item existingItem, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			
			if(existingItem == null) return true;

			// see if the modified date is different in any version (because disk is master, ANY changes we want to force overwrite)
			return serializedItem.Versions.Any(version =>
				{
					Item targetVersion = existingItem.Database.GetItem(existingItem.ID, Language.Parse(version.Language), Sitecore.Data.Version.Parse(version.VersionNumber));
					var serializedModified = version.Fields[FieldIDs.Updated.ToString()];
					
					if (!string.IsNullOrEmpty(serializedModified))
					{
						var itemModified = targetVersion[FieldIDs.Updated.ToString()];
						var result = !serializedModified.Equals(itemModified, StringComparison.OrdinalIgnoreCase);

						if (result)
						{
							var dtSerialized = DateUtil.IsoDateToDateTime(serializedModified);
							var dtItem = DateUtil.IsoDateToDateTime(itemModified);

							progress.ReportStatus(
								string.Format("{0} ({1} #{2}): Disk modified {3}, Item modified {4}", serializedItem.ItemPath, version.Language,
											version.VersionNumber, dtSerialized.ToString(), dtItem.ToString()), MessageType.Debug);
						}

						return result;
					}

					// ocasionally a version will not have a modified date, only a revision so we compare those as a backup
					var serializedRevision = version.Fields[FieldIDs.Revision.ToString()];
					
					if (!string.IsNullOrEmpty(serializedRevision))
					{
						var itemRevision = targetVersion.Statistics.Revision;
						var result = serializedRevision.Equals(itemRevision, StringComparison.OrdinalIgnoreCase);

						if (result)
							progress.ReportStatus(
								string.Format("{0} ({1} #{2}): Disk revision {3}, Item revision {4}", serializedItem.ItemPath, version.Language,
											version.VersionNumber, serializedRevision, itemRevision), MessageType.Debug);

						return result;
					}

					// if we get here we have no valid updated or revision to compare. Let's ignore the item as if it was a real item it'd have one of these.
					if (!serializedItem.ItemPath.StartsWith("/sitecore/templates/System") && !serializedItem.ItemPath.StartsWith("/sitecore/templates/Sitecore Client")) // this occurs a lot in stock system templates - we ignore warnings for those as it's expected.
						progress.ReportStatus(string.Format("{0} ({1} #{2}): Serialized version had no modified or revision field to check for update.", serializedItem.ItemPath, version.Language, version.VersionNumber), MessageType.Warning);

					return false;
				});
		}
	}
}
