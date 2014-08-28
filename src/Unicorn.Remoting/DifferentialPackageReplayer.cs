using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Engines;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;

namespace Unicorn.Remoting
{
	public class DifferentialPackageReplayer
	{
		private readonly RemotingPackage _package;

		public DifferentialPackageReplayer(RemotingPackage package)
		{
			_package = package;
			Assert.ArgumentNotNull(package, "package");
			Assert.IsTrue(package.Manifest.Strategy == RemotingStrategy.Differential, "Package must be differential to use the replayer.");
		}

		public bool Replay(ILogger logger)
		{
			bool disableNewSerialization = UnicornDataProvider.DisableSerialization;
			try
			{
				UnicornDataProvider.DisableSerialization = true;

				bool success = true;
				foreach (var action in _package.Manifest.HistoryEntries)
				{
					switch (action.Action)
					{
						case HistoryAction.Deleted:
							success = ProcessDeletion(action, logger) && success;
							break;
						case HistoryAction.Created:
						case HistoryAction.Saved:
						case HistoryAction.AddedVersion:
						case HistoryAction.RemovedVersion:
							success = ProcessUpdate(action, logger) && success;
							break;
						case HistoryAction.Moved:
							success = ProcessMoved(action, logger) && success;
							break;
						default:
							logger.Warn("Unknown history entry {0} found for {1} - cannot process.".FormatWith(action.Action.ToString(), action.ItemPath));
							break;
					}
				}

				return success;
			}
			finally
			{
				UnicornDataProvider.DisableSerialization = disableNewSerialization;
			}
		}

		private bool ProcessDeletion(RemotingPackageManifestEntry action, ILogger logger)
		{
			var currentItem = Factory.GetDatabase(action.Database).GetItem(new ID(action.ItemId));

			if (currentItem == null)
			{
				logger.Error("Cannot delete {0} because it had already been deleted.".FormatWith(action.ItemPath));
				return false;
			}

			currentItem.Delete();
			logger.Warn("[D] {0} [remoting]".FormatWith(new SitecoreSourceItem(currentItem).DisplayIdentifier));

			return true;
		}

		private bool ProcessMoved(RemotingPackageManifestEntry action, ILogger logger)
		{
			var database = Factory.GetDatabase(action.Database);

			Assert.IsNotNull(database, "Invalid database " + action.Database);

			var currentItem = database.GetItem(new ID(action.ItemId));

			if (currentItem == null)
			{
				logger.Error("Cannot move {0} because it has been deleted.".FormatWith(action.ItemPath));
				return false;
			}

			var newParentPath = action.ItemPath.Substring(0, action.ItemPath.LastIndexOf('/'));
			var newParent = database.GetItem(newParentPath);

			if (newParent == null)
			{
				logger.Error("Cannot move {0} because the new parent path {1} is not a valid item.".FormatWith(action.OldItemPath, newParentPath));
				return false;
			}

			currentItem.MoveTo(newParent);
			logger.Info("[M] {0} to {1} [remoting]".FormatWith(new SitecoreSourceItem(currentItem).DisplayIdentifier, new SitecoreSourceItem(newParent).DisplayIdentifier));
			return true;
		}

		private bool ProcessUpdate(RemotingPackageManifestEntry action, ILogger logger)
		{
			var serializedItem = _package.SerializationProvider.GetItemByPath(action.Database, action.ItemPath);

			if (serializedItem == null)
			{
				logger.Error("Corrupted package: expected serialized item for update {0} did not exist".FormatWith(action.ItemPath));
				return false;
			}

			serializedItem.Deserialize(false);

			var actionTaken = action.Action == HistoryAction.Created ? "[A]" : "[U]";
			logger.Info("{0} {1} [remoting]".FormatWith(actionTaken, serializedItem.DisplayIdentifier));
			return true;
		}
	}
}
