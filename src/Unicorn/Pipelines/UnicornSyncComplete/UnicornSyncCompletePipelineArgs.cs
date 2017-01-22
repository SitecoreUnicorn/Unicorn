using System;
using System.Collections.ObjectModel;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	/// <summary>
	/// Pipeline runs when a single configuration finishes syncing (e.g. if syncing 4 configs this runs 4 times)
	/// Not run if config errors.
	/// </summary>
	public class UnicornSyncCompletePipelineArgs : PipelineArgs
	{
		public UnicornSyncCompletePipelineArgs(IConfiguration configuration, DateTime syncStartedTimestamp)
		{
			Assert.ArgumentNotNull(configuration, "configuration");

			var dataCollector = configuration.Resolve<ISyncCompleteDataCollector>();

			Assert.IsNotNull(dataCollector, "Configuration had no ISyncCompleteDataCollector registered!");

			Changes = dataCollector.GetChanges();
			Configuration = configuration;
			SyncStartedTimestamp = syncStartedTimestamp;
			ProcessedItemCount = dataCollector.ProcessedItemCount;
		}

		public ReadOnlyCollection<ChangeEntry> Changes { get; private set; }
		public int ProcessedItemCount { get; private set; }
		public IConfiguration Configuration { get; private set; }
		public DateTime SyncStartedTimestamp { get; private set; }
	}
}
