using Unicorn.Loader;

namespace Unicorn.Tests.Loader
{
	class TestSyncConfiguration : ISyncConfiguration
	{
		public bool UpdateLinkDatabase => false;
		public bool UpdateSearchIndex => false;
		public int MaxConcurrency => 1;
	}
}
