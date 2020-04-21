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
	/// See http://www.velir.com/blog/index.php/2013/11/22/how-to-create-a-custom-publish-queue-in-sitecore/ among other sources
	/// </summary>
	public class ManualPublishQueueHandler : PublishProcessor
	{
		private static readonly ConcurrentQueue<ID> ManuallyAddedCandidates = new ConcurrentQueue<ID>();
		protected static bool UsePublishManager = Settings.GetBoolSetting("Unicorn.UsePublishManager", true);
		protected static bool UsePublishingService = Settings.GetBoolSetting("Unicorn.UsePublishingService", false);
		protected static int PublishingServiceMaxItemsToQueue = Settings.GetIntSetting("Unicorn.PublishingServiceMaxItemsToQueue", 50);

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

			if (!UsePublishingService)
			{
				foreach (var database in targets)
				{
					logger?.Debug($"> Publishing {ManuallyAddedCandidates.Count} synced item{suffix} in queue to {database.Name}");
					var publishOptions = new PublishOptions(triggerItem.Database, database, PublishMode.SingleItem, triggerItem.Language, DateTime.UtcNow) { RootItem = triggerItem, CompareRevisions = compareRevisions, RepublishAll = true };
					if (UsePublishManager)
					{
						// this works much faster then `new Publisher(publishOptions, triggerItem.Database.Languages).PublishWithResult();`
						var handle = PublishManager.Publish(new PublishOptions[] { publishOptions });
						var publishingSucces = PublishManager.WaitFor(handle);

						if (publishingSucces)
						{
							logger?.Debug($"> Published synced item{suffix} to {database.Name}. Statistics is not retrievable when Publish Manager is used (see setting Unicorn.UsePublishManager comments).");
						}
						else
						{
							logger?.Error($"> Error happened during publishing. Check Sitecore logs for details.");
						}

					}
					else
					{
						var result = new Publisher(publishOptions, triggerItem.Database.Languages).PublishWithResult();

						logger?.Debug($"> Published synced item{suffix} to {database.Name} (New: {result.Statistics.Created}, Updated: {result.Statistics.Updated}, Deleted: {result.Statistics.Deleted} Skipped: {result.Statistics.Skipped})");
					}
				}
			}
			else
			{
				var counter = 0;
				var triggerItemDatabase = triggerItem.Database;
				var deepModePublish = false;
				var publishRelatedItems = false;

				logger?.Debug($"> Queueing {ManuallyAddedCandidates.Count} synced item{suffix} in publishing service.");

				if (ManuallyAddedCandidates.Count <= PublishingServiceMaxItemsToQueue)
				{
					// using publishing service to manually queue items in publishing service
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

					logger?.Debug($"> Queued {counter} synced item{suffix} in publishing service.");
				} 
				else
				{
					// we have more than maxItemsToQueue
					PublishManager.PublishSmart(triggerItemDatabase, targets, triggerItemDatabase.Languages);
					logger?.Debug($"> Since we have more than {PublishingServiceMaxItemsToQueue} synced items - it is counter-productive to queue them one-by-one, so we are publishing whole database to all targets.");
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
