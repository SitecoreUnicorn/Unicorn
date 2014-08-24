using System.Collections.Concurrent;
using System.Collections.Generic;
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

		public static void AddItemToPublish(ID itemId)
		{
			ManuallyAddedCandidates.Enqueue(itemId);
		}

		public static bool PublishQueuedItems(Item triggerItem, Database[] targets)
		{
			if (ManuallyAddedCandidates.Count == 0) return false;

			// the trigger item simply has to exist so the publish occurs - our queue will then be injected
			PublishManager.PublishItem(triggerItem, targets, new[] { triggerItem.Language }, true, false);

			return true;
		}

		public override void Process(PublishContext context)
		{
			var candidates = new List<PublishingCandidate>();

			ID candidate;
			do
			{
				if (!ManuallyAddedCandidates.TryDequeue(out candidate)) break;

				candidates.Add(new PublishingCandidate(candidate, context.PublishOptions));
			} while (candidate != (ID)null);

			context.Queue.Add(candidates);
		}
	}
}
