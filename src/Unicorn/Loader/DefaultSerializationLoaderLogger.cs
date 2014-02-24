using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public class DefaultSerializationLoaderLogger : ISerializationLoaderLogger
	{
		private readonly ILogger _logger;

		public DefaultSerializationLoaderLogger(ILogger logger)
		{
			_logger = logger;
		}

		public void BeginLoadingTree(ISerializedReference rootSerializedItem)
		{
			_logger.Info("Unicorn loading {0}".FormatWith(rootSerializedItem.DisplayIdentifier, rootSerializedItem.ProviderId));
			_logger.Debug("Provider root ID: {0}".FormatWith(rootSerializedItem.ProviderId));
		}
		
		public void EndLoadingTree(ISerializedReference rootSerializedItem, int itemsProcessed, long elapsedMilliseconds)
		{
			_logger.Info("Unicorn completed loading {0}".FormatWith(rootSerializedItem.DisplayIdentifier));
			_logger.Debug("Items processed: {0}, Elapsed time: {1}ms ({2:N2}ms/item)".FormatWith(itemsProcessed, elapsedMilliseconds, (double)elapsedMilliseconds/(double)itemsProcessed));
		}

		public void SkippedItemPresentInSerializationProvider(ISerializedReference root, string predicateName, string serializationProviderName, string justification)
		{
			_logger.Warn("[S] {0} by {1} but it was in {2}. {3}<br />This usually indicates an extraneous excluded serialized item is present in the {3}, which should be removed.".FormatWith(root.DisplayIdentifier, predicateName, serializationProviderName, justification));
		}

		public void SkippedItemMissingInSerializationProvider(ISerializedReference item, string serializationProviderName)
		{
			_logger.Warn("[S] {0}. Unable to get a serialized item for the path. <br />This usually indicates an orphaned serialized item tree in {1} which should be removed. <br />Less commonly, it could also indicate a sparsely serialized tree which is not supported.".FormatWith(item.DisplayIdentifier, serializationProviderName));
		}

		public void SkippedItem(ISourceItem skippedItem, string predicateName, string justification)
		{
			_logger.Debug("[S] {0} (and children) by {1}: {2}".FormatWith(skippedItem.DisplayIdentifier, predicateName, justification));
		}
	}
}
