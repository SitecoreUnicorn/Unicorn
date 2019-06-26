using Rainbow.Model;
using Sitecore.Pipelines;
using Unicorn.Configuration;
using Unicorn.Logging;

namespace Unicorn.Pipelines.UnicornReserializeStart
{
	public class UnicornReserializeStartPipelineArgs : PipelineArgs, IUnicornOperationStartPipelineArgs
	{
		public UnicornReserializeStartPipelineArgs(OperationType type, IConfiguration[] configurations, ILogger logger, IItemData partialOperationRoot)
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
