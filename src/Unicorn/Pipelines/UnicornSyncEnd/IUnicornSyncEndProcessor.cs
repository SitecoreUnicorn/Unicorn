namespace Unicorn.Pipelines.UnicornSyncEnd
{
	public interface IUnicornSyncEndProcessor
	{
		void Process(UnicornSyncEndPipelineArgs args);
	}
}
