using Kamsar.WebConsole;
using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncEnd
{
	/// <summary>
	/// Pipeline runs when a whole batch sync configuration finishes sync (e.g. if syncing 4 configs this runs 1 time)
	/// This pipeline runs regardless of any errors in sync. Success is indicated by a flag.
	/// </summary>
	public class UnicornSyncEndPipelineArgs : PipelineArgs
	{
		public UnicornSyncEndPipelineArgs(IProgressStatus console, bool succeeded, params IConfiguration[] syncedConfigurations)
		{
			Console = console;
			SyncedConfigurations = syncedConfigurations;
			Succeeded = succeeded;
		}

		public IProgressStatus Console { get; }

		public IConfiguration[] SyncedConfigurations { get; }

		public bool Succeeded { get; }
	}
}
