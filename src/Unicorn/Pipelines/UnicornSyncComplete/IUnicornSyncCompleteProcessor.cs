namespace Unicorn.Pipelines.UnicornSyncComplete
{
	public interface IUnicornSyncCompleteProcessor
	{
		void Process(UnicornSyncCompletePipelineArgs args);
	}
}
