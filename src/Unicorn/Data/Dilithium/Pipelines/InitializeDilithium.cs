using System;
using Unicorn.Pipelines.UnicornSyncStart;

namespace Unicorn.Data.Dilithium.Pipelines
{
	public class InitializeDilithium : IUnicornSyncStartProcessor
	{
		public void Process(UnicornSyncStartPipelineArgs args)
		{
			args.Logger.Debug("Dilithium is batching items. Hold on to your hat, it's about to get fast in here...");

			ReactorContext.Reactor = new DilithiumReactor(args.Configurations);
			bool inited = ReactorContext.Reactor.Initialize(false);

			if(!inited) args.Logger.Debug("No configurations slated to sync enabled Dilithium. Sitecore APIs will be used.");
		}
	}
}
