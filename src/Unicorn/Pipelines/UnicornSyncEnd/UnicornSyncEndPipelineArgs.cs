using Kamsar.WebConsole;
using Sitecore.Pipelines;
using Unicorn.Configuration;
using Unicorn.Logging;

namespace Unicorn.Pipelines.UnicornSyncEnd
{
	/// <summary>
	/// Pipeline runs when a whole batch sync configuration finishes sync (e.g. if syncing 4 configs this runs 1 time)
	/// This pipeline runs regardless of any errors in sync. Success is indicated by a flag.
	/// </summary>
	public class UnicornSyncEndPipelineArgs : PipelineArgs
	{
		public UnicornSyncEndPipelineArgs(ILogger logger, bool succeeded, params IConfiguration[] syncedConfigurations)
		{
			Logger = logger;
			SyncedConfigurations = syncedConfigurations;
			Succeeded = succeeded;
		}

		public ILogger Logger { get; }

		public IConfiguration[] SyncedConfigurations { get; }

		public bool Succeeded { get; }
	}
}
