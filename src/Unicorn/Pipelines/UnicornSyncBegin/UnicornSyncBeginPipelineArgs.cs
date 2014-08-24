using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncBegin
{
	public class UnicornSyncBeginPipelineArgs : PipelineArgs
	{
		public UnicornSyncBeginPipelineArgs(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; private set; }
	}
}
