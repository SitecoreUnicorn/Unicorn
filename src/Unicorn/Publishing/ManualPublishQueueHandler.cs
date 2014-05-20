using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Publishing
{
	public class ManualPublishQueueHandler : PublishProcessor
	{
		private static readonly ConcurrentQueue<PublishingCandidate> ManuallyAddedCandidates = new ConcurrentQueue<PublishingCandidate>();
		private static readonly ConcurrentDictionary<string, Database[]> PublishingTargets = new ConcurrentDictionary<string, Database[]>(); 

		public static void QueueSerializedItem(ISerializedItem serializedItem)
		{
			foreach (var target in GetPublishingTargets(serializedItem.DatabaseName))
			{
				foreach (var language in serializedItem.Versions.Select(x => x.Language).Distinct().Select(Language.Parse))
				{
					AddItemToPublish(serializedItem.Id, Factory.GetDatabase(serializedItem.DatabaseName), target, language, false);
				}
			}
		}

		public static void QueueSourceItem(ISourceItem sourceItem)
		{
			foreach (var target in GetPublishingTargets(sourceItem.DatabaseName))
			{
				foreach (var language in sourceItem.Versions.Select(x => x.Language).Distinct().Select(Language.Parse))
				{
					AddItemToPublish(sourceItem.Id, Factory.GetDatabase(sourceItem.DatabaseName), target, language, false);
				}
			}
		}

		private static IEnumerable<Database> GetPublishingTargets(string database)
		{
			Database[] databases;
			if (!PublishingTargets.TryGetValue(database, out databases))
			{
				databases = PublishManager.GetPublishingTargets(Factory.GetDatabase(database)).Select(x => Factory.GetDatabase(x["Target database"])).ToArray();
				PublishingTargets.TryAdd(database, databases);
			}

			return databases;	
		}

		public static void AddItemToPublish(ID itemId, Database database, Database targetDatabase, Language language, bool deep)
		{
			var options = new PublishOptions(database, targetDatabase, PublishMode.SingleItem, language, DateTime.Now);
			options.Deep = deep;

			ManuallyAddedCandidates.Enqueue(new PublishingCandidate(itemId, options));
		}

		public static void Process()
		{
			if(ManuallyAddedCandidates.Count == 0) return;

			var sourceDb = Factory.GetDatabase("master");
			var options = new PublishOptions(sourceDb, Factory.GetDatabase("web"), PublishMode.Full, Language.Parse("en"), DateTime.UtcNow);
			options.RootItem = sourceDb.GetRootItem();
			options.Deep = false;

			var publisher = new Publisher(options);

			publisher.Publish();
		}

		public override void Process(PublishContext context)
		{
			var candidates = new List<PublishingCandidate>();

			PublishingCandidate candidate;
			do
			{
				if (!ManuallyAddedCandidates.TryDequeue(out candidate)) break;

				candidates.Add(candidate);
			} while (candidate != null);

			context.Queue.Add(candidates);
		}
	}
}
