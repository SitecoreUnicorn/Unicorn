using System;
using System.Collections.Concurrent;
using System.Linq;
using Kamsar.WebConsole;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;

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

		public static bool PublishQueuedItems(Item triggerItem, Database[] targets, IProgressStatus progress = null)
		{
			if (ManuallyAddedCandidates.Count == 0) return false;

			foreach (var database in targets)
			{
				progress?.ReportStatus("> Publishing {0} synced item{2} in queue to {1}", MessageType.Debug, ManuallyAddedCandidates.Count, database.Name, ManuallyAddedCandidates.Count == 1 ? string.Empty : "s");

				var publishOptions = new PublishOptions(triggerItem.Database, database, PublishMode.SingleItem, triggerItem.Language, DateTime.UtcNow) { RootItem = triggerItem, CompareRevisions = false, RepublishAll = true };

				var result = new Publisher(publishOptions, triggerItem.Database.Languages).PublishWithResult();

				progress?.ReportStatus("> Published synced items to {0} (New: {1}, Updated: {2}, Deleted: {3} Skipped: {4})", MessageType.Debug, database.Name, result.Statistics.Created, result.Statistics.Updated, result.Statistics.Deleted, result.Statistics.Skipped);
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
