using System.Diagnostics.CodeAnalysis;
using Rainbow.Model;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;

namespace Unicorn.Loader
{
	[ExcludeFromCodeCoverage]
	public class DefaultSerializationLoaderLogger : ISerializationLoaderLogger
	{
		private readonly ILogger _logger;

		public DefaultSerializationLoaderLogger(ILogger logger)
		{
			_logger = logger;
		}

		public virtual void BeginLoadingTree(IItemData rootSerializedItemData)
		{
			
		}
		
		public virtual void EndLoadingTree(IItemData rootSerializedItemData, int itemsProcessed, long elapsedMilliseconds)
		{
			
		}

		public virtual void SkippedItemPresentInSerializationProvider(IItemData root, string predicateName, string serializationProviderName, string justification)
		{
			_logger.Warn("[S] {0} by {1} but it was in {2}. {3}<br />This usually indicates an extraneous excluded serialized item is present in the {3}, which should be removed.".FormatWith(root.GetDisplayIdentifier(), predicateName, serializationProviderName, justification));
		}

		public virtual void SkippedItem(IItemData skippedItemData, string predicateName, string justification)
		{
			
		}
	}
}
