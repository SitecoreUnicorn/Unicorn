using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Unicorn.Remoting
{
	public class RemotingPackageManifest
	{
		private readonly List<RemotingPackageManifestEntry> _entries = new List<RemotingPackageManifestEntry>();

		public RemotingPackageManifest()
		{
			LastSynchronized = DateTime.UtcNow;
		}

		public void AddEntry(RemotingPackageManifestEntry entry)
		{
			_entries.Add(entry);
		}

		public DateTime LastSynchronized { get; set; }
		public RemotingPackageManifestEntry[] HistoryEntries { get { return _entries.ToArray(); } }

		public void WriteToPackage(string tempDirectory)
		{
			File.WriteAllText(Path.Combine(tempDirectory, "manifest.json"), JsonConvert.SerializeObject(this));
		}
	}
}
