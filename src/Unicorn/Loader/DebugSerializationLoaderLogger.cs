using System.Diagnostics.CodeAnalysis;
using Rainbow.Model;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;

namespace Unicorn.Loader
{
	/// <summary>
	/// Loader logger that dumps additional debug data about loading to the logs
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class DebugSerializationLoaderLogger : DefaultSerializationLoaderLogger
	{
		private readonly ILogger _logger;

		public DebugSerializationLoaderLogger(ILogger logger) : base(logger)
		{
			_logger = logger;
		}

		public override void BeginLoadingTree(IItemData rootSerializedItemData)
		{
			_logger.Info("Unicorn loading {0}".FormatWith(rootSerializedItemData.GetDisplayIdentifier()));
		}

		public override void EndLoadingTree(IItemData rootSerializedItemData, int itemsProcessed, long elapsedMilliseconds)
		{
			_logger.Info("Unicorn completed loading {0}".FormatWith(rootSerializedItemData.GetDisplayIdentifier()));
			_logger.Debug("Items processed: {0}, Elapsed time: {1}ms ({2:N2}ms/item)".FormatWith(itemsProcessed, elapsedMilliseconds, (double)elapsedMilliseconds / (double)itemsProcessed));
		}

		public override void SkippedItem(IItemData skippedItemData, string predicateName, string justification)
		{
			_logger.Debug("[S] {0} (and children) by {1}: {2}".FormatWith(skippedItemData.GetDisplayIdentifier(), predicateName, justification));
		}
	}
}
