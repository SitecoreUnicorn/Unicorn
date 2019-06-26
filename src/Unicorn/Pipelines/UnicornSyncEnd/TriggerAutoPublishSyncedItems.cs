using System.Collections.Generic;
using System.Linq;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Unicorn.Publishing;

namespace Unicorn.Pipelines.UnicornSyncEnd
{
	/// <summary>
	/// Triggers an auto-publish of the synced items in the processed configurations
	/// </summary>
	public class TriggerAutoPublishSyncedItems : IUnicornSyncEndProcessor
	{
		public string PublishTriggerItemId { get; set; }

		private readonly List<string> _targetDatabases = new List<string>();

		public void AddTargetDatabase(string database)
		{
			_targetDatabases.Add(database);
		}

		public void Process(UnicornSyncEndPipelineArgs args)
		{
			Assert.IsNotNullOrEmpty(PublishTriggerItemId, "Must set PublishTriggerItemId parameter.");

			if (_targetDatabases == null || _targetDatabases.Count == 0) return;

			if (!ManualPublishQueueHandler.HasItemsToPublish) return;

			// this occurs prior to the SerializationComplete event, which clears caches
			// if this is not done here, old content can be published that is out of date
			// particularly unversioned fields
			CacheManager.ClearAllCaches();

			var dbs = _targetDatabases.Select(Factory.GetDatabase).ToArray();
			var trigger = Factory.GetDatabase("master").GetItem(PublishTriggerItemId);

			Assert.IsTrue(dbs.Length > 0, "No valid databases specified to publish to.");
			Assert.IsNotNull(trigger, "Invalid trigger item ID");

			args.Logger.Info(string.Empty);
			args.Logger.Info("[P] Auto-publishing of synced items is beginning.");
			Log.Info("Unicorn: initiated synchronous publishing of synced items.", this);

			if (ManualPublishQueueHandler.PublishQueuedItems(trigger, dbs, args.Logger))
			{
				Log.Info("Unicorn: publishing of synced items is complete.", this);
			}
		}
	}
}
