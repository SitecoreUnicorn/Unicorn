using System;

namespace Unicorn.Loader
{
	public class DefaultSyncConfiguration : ISyncConfiguration
	{
		public DefaultSyncConfiguration(bool updateLinkDatabase, bool updateSearchIndex, int maxConcurrency)
		{
			UpdateLinkDatabase = updateLinkDatabase;
			UpdateSearchIndex = updateSearchIndex;

			if (maxConcurrency < 1) throw new InvalidOperationException("Max concurrency is set to zero. Please set it to one or more threads.");

			MaxConcurrency = maxConcurrency;
		}

		public bool UpdateLinkDatabase { get; }
		public bool UpdateSearchIndex { get; }
		public int MaxConcurrency { get; }
	}
}
