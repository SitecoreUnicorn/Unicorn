namespace Unicorn.Loader
{
	public class DefaultSyncConfiguration : ISyncConfiguration
	{
		public DefaultSyncConfiguration(bool updateLinkDatabase, bool updateSearchIndex, int maxConcurrency)
		{
			UpdateLinkDatabase = updateLinkDatabase;
			UpdateSearchIndex = updateSearchIndex;
			MaxConcurrency = maxConcurrency;
		}

		public bool UpdateLinkDatabase { get; }
		public bool UpdateSearchIndex { get; }
		public int MaxConcurrency { get; }
	}
}
