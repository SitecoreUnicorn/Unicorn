using System.Linq;
using Kamsar.WebConsole;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public class ConsoleSerializationLoaderLogger : ISerializationLoaderLogger
	{
		private readonly IProgressStatus _progress;

		public ConsoleSerializationLoaderLogger(IProgressStatus progress)
		{
			_progress = progress;
		}

		public void BeginLoadingTree(ISerializedReference rootSerializedItem)
		{
			_progress.ReportStatus("Loading serialized items under {0}<br />Provider root ID: {1}", MessageType.Debug, rootSerializedItem.DisplayIdentifier, rootSerializedItem.ProviderId);
		}
		
		public void EndLoadingTree(ISerializedReference rootSerializedItem, int itemsProcessed, long elapsedMilliseconds)
		{
			_progress.ReportStatus("Completed Loading serialized items under {0}<br />Items processed: {2}, Elapsed time: {3}ms", MessageType.Debug, rootSerializedItem.DisplayIdentifier, rootSerializedItem.ProviderId, itemsProcessed, elapsedMilliseconds);
		}

		public void SkippedItemPresentInSerializationProvider(ISerializedReference root, string predicateName, string serializationProviderName, string justification)
		{
			_progress.ReportStatus("[S] {0} by {1} but it was in {2}. {3}<br />This usually indicates an extraneous excluded serialized item is present in the {3}, which should be removed.", MessageType.Warning, root.DisplayIdentifier, predicateName, serializationProviderName, justification);
		}


		public void SkippedItemMissingInSerializationProvider(ISerializedReference item, string serializationProviderName)
		{
			_progress.ReportStatus("[S] {0}. Unable to get a serialized item for the path. <br />This usually indicates an orphaned serialized item tree in {1} which should be removed. <br />Less commonly, it could also indicate a sparsely serialized tree which is not supported.", MessageType.Warning, item.DisplayIdentifier, serializationProviderName);
		}


		public void SkippedItem(ISourceItem skippedItem, string predicateName, string justification)
		{
			_progress.ReportStatus("[S] {0} (and children) by {1}: {2}", MessageType.Debug, skippedItem.DisplayIdentifier, predicateName, justification);
		}


		public void SerializedNewItem(ISerializedItem serializedItem)
		{
			_progress.ReportStatus("[A] {1}", MessageType.Info, serializedItem.DisplayIdentifier);
		}

		public void SerializedUpdatedItem(ISerializedItem serializedItem)
		{
			_progress.ReportStatus("[U] {1}", MessageType.Info, serializedItem.DisplayIdentifier);
		}


		
	}
}
