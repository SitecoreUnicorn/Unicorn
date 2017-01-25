using Sitecore.Pipelines;
using Unicorn.Configuration;
using Unicorn.Logging;

namespace Unicorn.Pipelines.UnicornOperationStart
{
	/// <summary>
	/// Pipeline runs when a batch sync begins (e.g. if syncing 4 configs this runs 1 time)
	/// </summary>
	public class UnicornOperationStartPipelineArgs : PipelineArgs
	{
		public UnicornOperationStartPipelineArgs(IConfiguration[] configurations, ILogger logger)
		{
			Configurations = configurations;
			Logger = logger;
		}

		public IConfiguration[] Configurations { get; }
		public ILogger Logger { get; }
	}
}
