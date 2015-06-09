using Rainbow.Model;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;

namespace Unicorn.Loader
{
	public class DebugSerializationLoaderLogger : DefaultSerializationLoaderLogger
	{
		private readonly ILogger _logger;

		public DebugSerializationLoaderLogger(ILogger logger) : base(logger)
		{
			_logger = logger;
		}

		public override void BeginLoadingTree(ISerializableItem rootSerializedItem)
		{
			_logger.Info("Unicorn loading {0}".FormatWith(rootSerializedItem.GetDisplayIdentifier()));
		}

		public override void EndLoadingTree(ISerializableItem rootSerializedItem, int itemsProcessed, long elapsedMilliseconds)
		{
			_logger.Info("Unicorn completed loading {0}".FormatWith(rootSerializedItem.GetDisplayIdentifier()));
			_logger.Debug("Items processed: {0}, Elapsed time: {1}ms ({2:N2}ms/item)".FormatWith(itemsProcessed, elapsedMilliseconds, (double)elapsedMilliseconds / (double)itemsProcessed));
		}

		public override void SkippedItem(ISerializableItem skippedItem, string predicateName, string justification)
		{
			_logger.Debug("[S] {0} (and children) by {1}: {2}".FormatWith(skippedItem.GetDisplayIdentifier(), predicateName, justification));
		}
	}
}
