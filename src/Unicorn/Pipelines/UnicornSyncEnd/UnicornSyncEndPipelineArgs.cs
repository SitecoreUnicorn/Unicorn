using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncEnd
{
	public class UnicornSyncEndPipelineArgs : PipelineArgs
	{
		public UnicornSyncEndPipelineArgs(params IConfiguration[] syncedConfigurations)
		{
			SyncedConfigurations = syncedConfigurations;
		}

		public IConfiguration[] SyncedConfigurations { get; private set; }
	}
}
