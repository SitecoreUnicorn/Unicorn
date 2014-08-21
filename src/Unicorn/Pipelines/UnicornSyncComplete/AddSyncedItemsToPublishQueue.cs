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
				if(item.SerializedItem != null) ManualPublishQueueHandler.AddItemToPublish(item.SerializedItem.Id);
				if(item.SourceItem != null) ManualPublishQueueHandler.AddItemToPublish(item.SourceItem.Id);
			}
		}
	}
}
