using System;
using System.Collections.Concurrent;
using System.Linq;
using Kamsar.WebConsole;
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

		public static void AddItemToPublish(Guid itemId)
		{
			ManuallyAddedCandidates.Enqueue(new ID(itemId));
		}

		public static bool HasItemsToPublish => ManuallyAddedCandidates.Count > 0;

		public static bool PublishQueuedItems(Item triggerItem, Database[] targets, ILogger logger = null)
		{
			if (ManuallyAddedCandidates.Count == 0) return false;

			foreach (var database in targets)
			{
				var suffix = ManuallyAddedCandidates.Count == 1 ? string.Empty : "s";
				logger?.Debug($"> Publishing {ManuallyAddedCandidates.Count} synced item{suffix} in queue to {database.Name}");

				var publishOptions = new PublishOptions(triggerItem.Database, database, PublishMode.SingleItem, triggerItem.Language, DateTime.UtcNow) { RootItem = triggerItem, CompareRevisions = false, RepublishAll = true };

				var result = new Publisher(publishOptions, triggerItem.Database.Languages).PublishWithResult();

				logger?.Debug($"> Published synced items to {database.Name} (New: {result.Statistics.Created}, Updated: {result.Statistics.Updated}, Deleted: {result.Statistics.Deleted} Skipped: {result.Statistics.Skipped})");
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
