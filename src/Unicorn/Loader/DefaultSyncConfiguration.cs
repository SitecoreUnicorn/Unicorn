using System;

namespace Unicorn.Loader
{
	public class DefaultSyncConfiguration : ISyncConfiguration
	{
		public DefaultSyncConfiguration(bool updateLinkDatabase, bool updateSearchIndex)
		{
			UpdateLinkDatabase = updateLinkDatabase;
			UpdateSearchIndex = updateSearchIndex;
		}

		public bool UpdateLinkDatabase { get; }
		public bool UpdateSearchIndex { get; }
	}
}
