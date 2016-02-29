using Sitecore;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
    public class RebuildLinkDatabaseForSyncedItems : IUnicornSyncCompleteProcessor
    {
        public void Process(UnicornSyncCompletePipelineArgs args)
        {
            foreach (var item in args.Changes)
            {
                if (item.ChangeType == ChangeType.Deleted)
                    Globals.LinkDatabase.RemoveReferences(args.ProcessorItem.InnerItem);
                else
                    Globals.LinkDatabase.UpdateReferences(args.ProcessorItem.InnerItem);
            }
        }
    }
}