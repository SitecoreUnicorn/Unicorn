using Sitecore.Pipelines;
using Unicorn.Configuration;
using Unicorn.Logging;

namespace Unicorn.Pipelines.UnicornSyncStart
{
	/// <summary>
	/// Pipeline runs when a batch sync begins (e.g. if syncing 4 configs this runs 1 time)
	/// </summary>
	public class UnicornSyncStartPipelineArgs : PipelineArgs
	{
		public UnicornSyncStartPipelineArgs(IConfiguration[] configurations, ILogger logger)
		{
			Configurations = configurations;
			Logger = logger;
		}

		public IConfiguration[] Configurations { get; }
		public ILogger Logger { get; }
	}
}
