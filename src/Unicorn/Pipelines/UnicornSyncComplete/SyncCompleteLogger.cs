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

			logger.Info("{0}: {1} item{5} modified ({2} added, {3} updated, {4} deleted)".FormatWith(args.Configuration.Name,
				args.Changes.Count,
				args.Changes.Count(x => x.ChangeType == ChangeType.Created),
				args.Changes.Count(x => x.ChangeType == ChangeType.Modified),
				args.Changes.Count(x => x.ChangeType == ChangeType.Deleted),
				args.Changes.Count != 1 ? "s" : string.Empty));
		}
	}
}
