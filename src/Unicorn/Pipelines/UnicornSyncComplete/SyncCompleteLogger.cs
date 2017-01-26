using System;
using System.Linq;
using Sitecore.StringExtensions;
using Unicorn.Logging;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	/// <summary>
	/// Logs the metrics about the sync to the current logger
	/// </summary>
	public class SyncCompleteLogger : IUnicornSyncCompleteProcessor
	{
		public void Process(UnicornSyncCompletePipelineArgs args)
		{
			var logger = args.Configuration.Resolve<ILogger>();
			
			if(logger == null) return;

			var durationInMs = (DateTime.Now - args.SyncStartedTimestamp).TotalMilliseconds;
			var msPerItem = (durationInMs/(args.ProcessedItemCount == 0 ? 1 : args.ProcessedItemCount)).ToString("N2");

			logger.Info("{0} sync complete: {1} item{2} evaluated, {3} item{4} modified ({5} added, {6} updated, {7} recycled) in {8}ms (~{9}ms/item).".FormatWith(
				args.Configuration.Name,
				args.ProcessedItemCount,
				args.ProcessedItemCount != 1 ? "s" : string.Empty,
				args.Changes.Count,
				args.Changes.Count != 1 ? "s" : string.Empty,
				args.Changes.Count(x => x.ChangeType == ChangeType.Created),
				args.Changes.Count(x => x.ChangeType == ChangeType.Modified),
				args.Changes.Count(x => x.ChangeType == ChangeType.Deleted),
				(int)durationInMs,
				msPerItem));
			logger.Info(string.Empty);
		}
	}
}
