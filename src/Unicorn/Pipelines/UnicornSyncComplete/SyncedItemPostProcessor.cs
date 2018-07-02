using System;
using System.Diagnostics;
using System.Linq;
using Sitecore;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using Sitecore.Data.Items;
using Unicorn.Configuration;
using Unicorn.Loader;
using Unicorn.Logging;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	/// <summary>
	/// For configurations that update the link DB, this will batch update links after a config syncs
	/// </summary>
	public class SyncedItemPostProcessor : IUnicornSyncCompleteProcessor
	{
		public string UnconditionalCacheClearing { get; set; }

		public void Process(UnicornSyncCompletePipelineArgs args)
		{
			if (!"true".Equals(UnconditionalCacheClearing, StringComparison.OrdinalIgnoreCase))
			{
				if (!NeedsPostProcessing(args)) return;
			}

			var logger = args.Configuration.Resolve<ILogger>();

			logger.Debug(string.Empty);
			logger.Debug("> Preparing to post-process synced items (links/indexes). May take some time depending on change count...");

			// carpet bomb the cache since it can be out of date after a sync (before the serialization complete event has fired, which is after this happens)
			CacheManager.ClearAllCaches();
			var dbs = Factory.GetDatabases();
			foreach(var db in dbs) db.Engines.TemplateEngine.Reset();

			logger.Debug("> Caches have been cleared and template engine reset. Resolving items...");

			// resolve changed items from the sync. Note: this may not perform the most awesome with huge syncs.
			// but it's better than keeping stale items in memory during the sync. Note also that deleted items
			// from the sync will not resolve, because...they are gone. So the post processing here will not occur.
			var items = args.Changes
				.Where(change => change.ChangeType != ChangeType.Deleted && change.Id.HasValue)
				.Select(change => dbs.FirstOrDefault(db => db.Name.Equals(change.DatabaseName))?.GetItem(new ID(change.Id.Value)))
				.Where(item => item != null)
				.ToArray();

			logger.Debug("> Post process items resolved.");

			PostProcessItems(items, args.Configuration);
		}

		protected virtual bool NeedsPostProcessing(UnicornSyncCompletePipelineArgs args)
		{
			if (args.Changes.Count == 0) return false;

			var syncConfiguration = args.Configuration.Resolve<ISyncConfiguration>();

			if (syncConfiguration != null && (syncConfiguration.UpdateLinkDatabase || syncConfiguration.UpdateSearchIndex)) return true;

			return false;
		}

		protected virtual void PostProcessItems(Item[] items, IConfiguration configuration)
		{
			var syncConfiguration = configuration.Resolve<ISyncConfiguration>();

			if (syncConfiguration == null) return;

			var logger = configuration.Resolve<ILogger>();

			if (syncConfiguration.UpdateLinkDatabase)
				UpdateLinkDatabase(items, logger);

			if (syncConfiguration.UpdateSearchIndex)
				UpdateSearchIndexes(items, logger);
		}

		protected virtual void UpdateLinkDatabase(Item[] items, ILogger logger)
		{
			logger?.Info("");
			logger?.Info("[L] Updating link database for changed items.");

			Stopwatch sw = new Stopwatch();
			sw.Start();

			foreach (var item in items)
			{
				Globals.LinkDatabase.UpdateReferences(item);

				// NOTE: we don't have a reference to deleted items. This means that due to the link DB API requiring an Item parameter, we can't really remove deleted items from the LDB.
			}

			sw.Stop();

			logger?.Debug($"> Updated {items.Length} items in the link database in {(sw.ElapsedMilliseconds / 1000):F2} sec");
		}

		protected virtual void UpdateSearchIndexes(Item[] items, ILogger logger)
		{
			logger?.Info("");
			logger?.Info("[I] Updating search indexes for changed items.");

			foreach (var index in ContentSearchManager.Indexes)
			{
				var changes = items.Select(change => new SitecoreItemUniqueId(change.Uri));

				IndexCustodian.IncrementalUpdate(index, changes);
			}

			logger?.Debug($"> Queued updates for {items.Length} items in the search indexes. Will run async.");
		}
	}
}