using Unicorn.Pipelines.UnicornSyncComplete;

namespace Unicorn.Pipelines.UnicornSyncBegin
{
	public class ResetSyncCompleteDataCollector : IUnicornSyncBeginProcessor
	{
		public void Process(UnicornSyncBeginPipelineArgs args)
		{
			var collector = args.Configuration.Resolve<ISyncCompleteDataCollector>();

			if (collector != null) collector.Reset();
		}
	}
}
