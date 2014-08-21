using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncEnd
{
	public class UnicornSyncEndPipelineArgs : PipelineArgs
	{
		public UnicornSyncEndPipelineArgs(IConfiguration[] syncedConfigurations)
		{
			SyncedConfigurations = syncedConfigurations;
		}

		public IConfiguration[] SyncedConfigurations { get; private set; }
	}
}
