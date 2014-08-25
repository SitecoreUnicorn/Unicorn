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

		public RemotingStrategy Strategy { get; set; }
		public string ConfigurationName { get; set; }
		public DateTime LastSynchronized { get; set; }

		public RemotingPackageManifestEntry[] HistoryEntries
		{
			get { return _entries.ToArray(); }
			set
			{
				_entries.Clear();
				_entries.AddRange(value);
			}
		}

		public void WriteToPackage(string tempDirectory)
		{
			File.WriteAllText(Path.Combine(tempDirectory, "manifest.json"), JsonConvert.SerializeObject(this));
		}
	}
}
