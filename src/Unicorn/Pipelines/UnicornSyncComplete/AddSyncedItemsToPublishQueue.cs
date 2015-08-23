using Unicorn.Publishing;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	/// <summary>
	/// Adds the changed items from the sync to the manual publish queue
	/// </summary>
	public class AddSyncedItemsToPublishQueue : IUnicornSyncCompleteProcessor
	{
		public void Process(UnicornSyncCompletePipelineArgs args)
		{
			foreach (var item in args.Changes)
			{
				if(item.Id != null) ManualPublishQueueHandler.AddItemToPublish(item.Id.Value);
			}
		}
	}
}
