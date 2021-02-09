using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.IO;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;
using Unicorn.Logging;

namespace Unicorn.Publishing
{
	/// <summary>
	/// Maintains a manual publish queue that arbitrary items can be added to
	/// See https://www.velir.com/blog/2013/11/22/how-create-custom-publish-queue-sitecore among other sources
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "ManuallyAddedCandidates is concurrent by design.")]
	public class ManualPublishQueueHandler : PublishProcessor
	{
		private static readonly ConcurrentQueue<ID> ManuallyAddedCandidates = new ConcurrentQueue<ID>();

		private static readonly object PersistenceLock = new object();

		protected static bool UsePublishManager = Settings.GetBoolSetting("Unicorn.UsePublishManager", true);

		protected static bool UsePublishingService = Settings.GetBoolSetting("Unicorn.UsePublishingService", false);

		protected static int PublishingServiceMaxItemsToQueue = Settings.GetIntSetting("Unicorn.PublishingServiceMaxItemsToQueue", 50);

		protected static string PublishQueuePersistenceFile = Settings.GetSetting("Unicorn.PublishQueuePersistenceFile", "~/App_Data/Unicorn/publishqueue.tmp");

		public static bool HasItemsToPublish => ManuallyAddedCandidates.Count > 0;

		public static void AddItemToPublish(Guid itemId)
		{
			lock (PersistenceLock)
			{
				ManuallyAddedCandidates.Enqueue(new ID(itemId));
			}
		}

		public static bool PublishQueuedItems(Item triggerItem, Database[] targets, ILogger logger = null)
		{
			bool result;
			lock (PersistenceLock)
			{
				result = PublishQueuedItemsInternal(triggerItem, targets, logger);
			}

			return result;
		}

		public static void Persist()
		{
			lock (PersistenceLock)
			{
				StringBuilder contentBuilder = new StringBuilder();
				foreach (ID id in ManuallyAddedCandidates.ToArray())
				{
					contentBuilder.AppendLine(id.Guid.ToString("N"));
				}

				FileUtil.WriteToFile(PublishQueuePersistenceFile, contentBuilder.ToString());
			}
		}

		public static bool LoadFromPersistentStore()
		{
			bool result = false;
			lock (PersistenceLock)
			{
				if (FileUtil.FileExists(PublishQueuePersistenceFile))
				{
					string content = FileUtil.ReadFromFile(PublishQueuePersistenceFile);
					string[] persistedIds = content.Split(
						new[] { Environment.NewLine },
						StringSplitOptions.RemoveEmptyEntries);
					foreach (string id in persistedIds)
					{
						if (Guid.TryParse(id, out Guid guidId))
						{
							ManuallyAddedCandidates.Enqueue(new ID(guidId));
							result = true;
						}
					}
				}
			}

			return result;
		}

		public override void Process(PublishContext context)
		{
			IEnumerable<PublishingCandidate> candidates = ManuallyAddedCandidates
				.ToArray()
				.Select(id => new PublishingCandidate(id, context.PublishOptions));

			context.Queue.Add(candidates);
		}

		protected static bool PublishQueuedItemsInternal(Item triggerItem, Database[] targets, ILogger logger = null)
		{
			if (ManuallyAddedCandidates.Count == 0)
			{
				return false;
			}

			string suffix = ManuallyAddedCandidates.Count == 1 ? string.Empty : "s";
			const bool CompareRevisions = false;

			if (!UsePublishingService)
			{
				foreach (Database database in targets)
				{
					logger?.Debug($"> Publishing {ManuallyAddedCandidates.Count} synced item{suffix} in queue to {database.Name}");
					PublishOptions publishOptions =
						new PublishOptions(
							triggerItem.Database,
							database,
							PublishMode.SingleItem,
							triggerItem.Language,
							DateTime.UtcNow)
							{
								RootItem = triggerItem, CompareRevisions = CompareRevisions, RepublishAll = true
							};
					if (UsePublishManager)
					{
						// this works much faster then `new Publisher(publishOptions, triggerItem.Database.Languages).PublishWithResult();`
						Handle handle = PublishManager.Publish(new[] { publishOptions });
						bool publishingSuccess = PublishManager.WaitFor(handle);

						if (publishingSuccess)
						{
							logger?.Debug($"> Published synced item{suffix} to {database.Name}. Statistics is not retrievable when Publish Manager is used (see setting Unicorn.UsePublishManager comments).");
						}
						else
						{
							logger?.Error("> Error happened during publishing. Check Sitecore logs for details.");
						}
					}
					else
					{
						PublishResult result = new Publisher(publishOptions, triggerItem.Database.Languages).PublishWithResult();

						logger?.Debug($"> Published synced item{suffix} to {database.Name} (New: {result.Statistics.Created}, Updated: {result.Statistics.Updated}, Deleted: {result.Statistics.Deleted} Skipped: {result.Statistics.Skipped})");
					}
				}
			}
			else
			{
				int counter = 0;
				Database triggerItemDatabase = triggerItem.Database;
				const bool DeepModePublish = false;
				const bool PublishRelatedItems = false;

				logger?.Debug($"> Queueing {ManuallyAddedCandidates.Count} synced item{suffix} in publishing service.");

				if (ManuallyAddedCandidates.Count <= PublishingServiceMaxItemsToQueue)
				{
					// using publishing service to manually queue items in publishing service
					while (ManuallyAddedCandidates.Count > 0)
					{
						ManuallyAddedCandidates.TryDequeue(out ID itemId);

						Item publishCandidateItem = triggerItemDatabase.GetItem(itemId);
						if (publishCandidateItem != null)
						{
							counter++;
							PublishManager.PublishItem(publishCandidateItem, targets, triggerItemDatabase.Languages, DeepModePublish, CompareRevisions, PublishRelatedItems);
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
				ManuallyAddedCandidates.TryDequeue(out _);
			}

			// delete queue persistence after we publish
			if (FileUtil.FileExists(PublishQueuePersistenceFile))
			{
				FileUtil.Delete(PublishQueuePersistenceFile);
			}

			return true;
		}
	}
}
