using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncBegin
{
	public class UnicornSyncBeginPipelineArgs : PipelineArgs
	{
		public UnicornSyncBeginPipelineArgs(IConfiguration configuration)
		{
			Configuration = configuration;
			SyncIsHandled = false;
		}

		public IConfiguration Configuration { get; private set; }
		public bool SyncIsHandled { get; set; }
	}
}
