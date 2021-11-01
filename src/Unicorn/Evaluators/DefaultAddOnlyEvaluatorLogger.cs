using System.Diagnostics.CodeAnalysis;
using Rainbow.Model;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncComplete;

namespace Unicorn.Evaluators
{
	[ExcludeFromCodeCoverage]
	public class DefaultAddOnlyEvaluatorLogger : DefaultSerializedAsMasterEvaluatorLogger, IAddOnlyEvaluatorLogger
	{
		public DefaultAddOnlyEvaluatorLogger(ILogger logger, ISyncCompleteDataCollector pipelineDataCollector) : base(logger, pipelineDataCollector)
		{
		}

		public override void TemplateChanged(IItemData sourceItem, IItemData targetItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");
			Assert.ArgumentNotNull(targetItem, "targetItem");

			Logger.Debug("> Template: Skipped '{0}'".FormatWith(TryResolveItemName(targetItem.DatabaseName, targetItem.TemplateId)));
		}

		public override void Renamed(IItemData sourceItem, IItemData targetItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");
			Assert.ArgumentNotNull(targetItem, "targetItem");

			Logger.Debug("> Name: Skipped '{0}'".FormatWith(targetItem.Name));
		}
	}
}