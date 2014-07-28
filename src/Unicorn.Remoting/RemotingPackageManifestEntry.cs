using System;
using Sitecore.Data.Engines;

namespace Unicorn.Remoting
{
	public class RemotingPackageManifestEntry
	{
		public static RemotingPackageManifestEntry FromEntry(HistoryEntry entry)
		{
			var manifest = new RemotingPackageManifestEntry();
			manifest.Action = entry.Action;
			manifest.ItemId = entry.ItemId.Guid;
			manifest.ItemPath = entry.ItemPath;

			return manifest;
		}

		public HistoryAction Action { get; set; }
		public Guid ItemId { get; set; }
		public string ItemPath { get; set; }
		public string OldItemPath { get; set; }
	}
}
