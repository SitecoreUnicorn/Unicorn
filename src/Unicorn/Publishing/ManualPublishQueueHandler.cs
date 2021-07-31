using System;
using System.Collections.Concurrent;
using System.Linq;
using Kamsar.WebConsole;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;
using Unicorn.Logging;

namespace Unicorn.Publishing
{
	/// <summary>
	/// Maintains a manual publish queue that arbitrary items can be added to
	/// See https://www.velir.com/blog/2013/11/22/how-create-custom-publish-queue-sitecore among other sources
	/// </summary>
	public class ManualPublishQueueHandler : PublishProcessor
	{
		private static readonly ConcurrentQueue<ID> ManuallyAddedCandidates = new ConcurrentQueue<ID>();
		protected static bool LegacyPublishing = Settings.GetBoolSetting("Unicorn.LegacyPublishing", false);
		protected static int MaxItemsToQueue = Settings.GetIntSetting("Unicorn.MaxItemsToQueue", 50);

		public static void AddItemToPublish(Guid itemId)
		{
			ManuallyAddedCandidates.Enqueue(new ID(itemId));
		}

		public static bool HasItemsToPublish => ManuallyAddedCandidates.Count > 0;

		public static bool PublishQueuedItems(Item triggerItem, Database[] targets, ILogger logger = null)
		{
			if (ManuallyAddedCandidates.Count == 0) return false;
			var suffix = ManuallyAddedCandidates.Count == 1 ? string.Empty : "s";
			var compareRevisions = false;

			if (LegacyPublishing)
			{
				foreach (var database in targets)
				{
					logger?.Debug($"> Publishing {ManuallyAddedCandidates.Count} synced item{suffix} in queue to {database.Name}");
					var publishOptions = new PublishOptions(triggerItem.Database, database, PublishMode.SingleItem, triggerItem.Language, DateTime.UtcNow) {RootItem = triggerItem, CompareRevisions = compareRevisions, RepublishAll = true};
					var result = new Publisher(publishOptions, triggerItem.Database.Languages).PublishWithResult();
					logger?.Debug($"> Published synced item{suffix} to {database.Name} (New: {result.Statistics.Created}, Updated: {result.Statistics.Updated}, Deleted: {result.Statistics.Deleted} Skipped: {result.Statistics.Skipped})");
				}
			} 
			else 
			{
				var counter = 0;
				var triggerItemDatabase = triggerItem.Database;
				var deepModePublish = false;
				var publishRelatedItems = false;

				logger?.Debug($"> Queueing {ManuallyAddedCandidates.Count} synced item{suffix}");

				if (ManuallyAddedCandidates.Count <= MaxItemsToQueue)
				{
					logger?.Debug($"Processing queue 1-by-1 since queue is {ManuallyAddedCandidates.Count} and Unicorn.MaxItemsToQueue is {MaxItemsToQueue}");

					while (ManuallyAddedCandidates.Count > 0)
					{
						ID itemId;
						ManuallyAddedCandidates.TryDequeue(out itemId);

						var publishCandidateItem = triggerItemDatabase.GetItem(itemId);
						if (publishCandidateItem != null)
						{
							counter++;
							PublishManager.PublishItem(publishCandidateItem, targets, triggerItemDatabase.Languages, deepModePublish, compareRevisions, publishRelatedItems);
						}
					}
				} 
				else
				{
					logger?.Debug($"Executing system-wide Smart Publish since queue {ManuallyAddedCandidates.Count} and Unicorn.MaxItemsToQueue is {MaxItemsToQueue}");
					PublishManager.PublishSmart(triggerItemDatabase, targets, triggerItemDatabase.Languages);
				}
			}

			// clear the queue after we publish
			while (ManuallyAddedCandidates.Count > 0)
			{
				ID fake;
				ManuallyAddedCandidates.TryDequeue(out fake);
			}

			return true;
		}

		public override void Process(PublishContext context)
		{
			var candidates = ManuallyAddedCandidates
				.ToArray()
				.Select(id => new PublishingCandidate(id, context.PublishOptions));

			context.Queue.Add(candidates);
		}
	}
}
