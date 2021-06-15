using Sitecore.Diagnostics;
using Sitecore.Pipelines;

using Unicorn.Publishing;

namespace Unicorn.Pipelines.Initialize
{
	public class ManualQueueLoad
	{
		public void Process(PipelineArgs args)
		{
			if (ManualPublishQueueHandler.LoadFromPersistentStore())
			{
				Log.Info("Loaded persisted unicorn queue.", this);
			}
		}
	}
}
