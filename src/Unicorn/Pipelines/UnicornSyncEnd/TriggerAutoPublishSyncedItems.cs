using System.Collections.Generic;
using System.Linq;
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

			var dbs = _targetDatabases.Select(Factory.GetDatabase).ToArray();
			var trigger = Factory.GetDatabase("master").GetItem(PublishTriggerItemId);

			Assert.IsTrue(dbs.Length > 0, "No valid databases specified to publish to.");
			Assert.IsNotNull(trigger, "Invalid trigger item ID");

			if (ManualPublishQueueHandler.PublishQueuedItems(trigger, dbs))
			{
				Log.Info("Unicorn: initiated publishing of synced items.", this);
			}
		}
	}
}
