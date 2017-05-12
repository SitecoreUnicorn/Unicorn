namespace Unicorn.Pipelines.UnicornSyncStart
{
	public interface IUnicornSyncStartProcessor
	{
		void Process(UnicornSyncStartPipelineArgs args);
	}
}
