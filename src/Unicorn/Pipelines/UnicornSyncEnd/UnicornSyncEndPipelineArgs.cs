using Kamsar.WebConsole;
using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncEnd
{
	public class UnicornSyncEndPipelineArgs : PipelineArgs
	{
		public UnicornSyncEndPipelineArgs(IProgressStatus console, params IConfiguration[] syncedConfigurations)
		{
			Console = console;
			SyncedConfigurations = syncedConfigurations;
		}

		public IProgressStatus Console { get; private set; }
		public IConfiguration[] SyncedConfigurations { get; private set; }
	}
}
