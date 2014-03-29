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

		public virtual void BeginLoadingTree(ISerializedReference rootSerializedItem)
		{
			
		}
		
		public virtual void EndLoadingTree(ISerializedReference rootSerializedItem, int itemsProcessed, long elapsedMilliseconds)
		{
			
		}

		public virtual void SkippedItemPresentInSerializationProvider(ISerializedReference root, string predicateName, string serializationProviderName, string justification)
		{
			_logger.Warn("[S] {0} by {1} but it was in {2}. {3}<br />This usually indicates an extraneous excluded serialized item is present in the {3}, which should be removed.".FormatWith(root.DisplayIdentifier, predicateName, serializationProviderName, justification));
		}

		public virtual void SkippedItemMissingInSerializationProvider(ISerializedReference item, string serializationProviderName)
		{
			_logger.Warn("[S] {0}. Unable to get a serialized item for the path. <br />This usually indicates an orphaned serialized item tree in {1} which should be removed. <br />Less commonly, it could also indicate a sparsely serialized tree which is not supported, or a serialized item that is named differently than its metadata.".FormatWith(item.DisplayIdentifier, serializationProviderName));
		}

		public virtual void SkippedItem(ISourceItem skippedItem, string predicateName, string justification)
		{
			
		}
	}
}
