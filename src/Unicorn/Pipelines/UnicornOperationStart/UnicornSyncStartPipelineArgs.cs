using Rainbow.Model;
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
		public UnicornOperationStartPipelineArgs(OperationType type, IConfiguration[] configurations, ILogger logger, IItemData partialOperationRoot)
		{
			Type = type;
			Configurations = configurations;
			Logger = logger;
			PartialOperationRoot = partialOperationRoot;
		}

		public OperationType Type { get; }
		public IConfiguration[] Configurations { get; }
		public ILogger Logger { get; }
		public IItemData PartialOperationRoot { get; }
	}
}
