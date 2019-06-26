using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornSyncBegin
{
	/// <summary>
	/// Pipeline runs when a single configuration begins to sync (e.g. if syncing 4 configs this runs 4 times)
	/// </summary>
	public class UnicornSyncBeginPipelineArgs : PipelineArgs
	{
		public UnicornSyncBeginPipelineArgs(IConfiguration configuration)
		{
			Configuration = configuration;
			SyncIsHandled = false;
		}

		public IConfiguration Configuration { get; private set; }
		public bool SyncIsHandled { get; set; }
	}
}
