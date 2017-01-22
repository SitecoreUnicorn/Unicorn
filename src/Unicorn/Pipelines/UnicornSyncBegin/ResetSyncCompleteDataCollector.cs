using Unicorn.Pipelines.UnicornSyncComplete;

namespace Unicorn.Pipelines.UnicornSyncBegin
{
	/// <summary>
	/// Resets the data of the SyncCompleteDataCollector (because when this pipeline fires we're starting sync of a configuration so it should all be zero)
	/// </summary>
	public class ResetSyncCompleteDataCollector : IUnicornSyncBeginProcessor
	{
		public void Process(UnicornSyncBeginPipelineArgs args)
		{
			var collector = args.Configuration.Resolve<ISyncCompleteDataCollector>();

			collector?.Reset();
		}
	}
}
