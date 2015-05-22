using Gibson.Model;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;

namespace Unicorn.Loader
{
	public class DefaultSerializationLoaderLogger : ISerializationLoaderLogger
	{
		private readonly ILogger _logger;

		public DefaultSerializationLoaderLogger(ILogger logger)
		{
			_logger = logger;
		}

		public virtual void BeginLoadingTree(ISerializableItem rootSerializedItem)
		{
			
		}
		
		public virtual void EndLoadingTree(ISerializableItem rootSerializedItem, int itemsProcessed, long elapsedMilliseconds)
		{
			
		}

		public virtual void SkippedItemPresentInSerializationProvider(ISerializableItem root, string predicateName, string serializationProviderName, string justification)
		{
			_logger.Warn("[S] {0} by {1} but it was in {2}. {3}<br />This usually indicates an extraneous excluded serialized item is present in the {3}, which should be removed.".FormatWith(root.GetDisplayIdentifier(), predicateName, serializationProviderName, justification));
		}

		public virtual void SkippedItem(ISerializableItem skippedItem, string predicateName, string justification)
		{
			
		}
	}
}
