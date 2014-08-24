using System.Collections.ObjectModel;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	public class UnicornSyncCompletePipelineArgs : PipelineArgs
	{
		public UnicornSyncCompletePipelineArgs(IConfiguration configuration)
		{
			Assert.ArgumentNotNull(configuration, "configuration");

			var dataCollector = configuration.Resolve<ISyncCompleteDataCollector>();

			Assert.IsNotNull(dataCollector, "Configuration had no ISyncCompleteDataCollector registered!");

			Changes = dataCollector.GetChanges();
			Configuration = configuration;
		}

		public ReadOnlyCollection<ChangeEntry> Changes { get; private set; } 
		public IConfiguration Configuration { get; private set; }
	}
}
