using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Rainbow.Model;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Pipelines;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Loader;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncBegin;
using Unicorn.Pipelines.UnicornSyncComplete;
using Unicorn.Pipelines.UnicornSyncStart;
using Unicorn.Predicates;

namespace Unicorn
{
	/// <summary>
	/// Utility class designed to make it a bit simpler to call primary serialization operations programmatically
	/// </summary>
	public class SerializationHelper
	{
		public int ThreadCount { get; set; } = Settings.GetIntSetting("Unicorn.MaximumWriteConcurrency", 16);

		public virtual IConfiguration[] GetConfigurationsForItem(IItemData item)
		{
			return UnicornConfigurationManager.Configurations.Where(configuration => configuration.Resolve<IPredicate>().Includes(item).IsIncluded).ToArray();
		}

		/// <returns>True if the tree was dumped, false if the root item was not included</returns>
		public virtual bool DumpTree(IItemData item, IConfiguration[] configurations = null)
		{
			using (new TransparentSyncDisabler())
			{
				if (configurations == null) configurations = GetConfigurationsForItem(item);

				foreach (var configuration in configurations)
				{
					if (configuration == null) return false;

					var logger = configuration.Resolve<ILogger>();

					var predicate = configuration.Resolve<IPredicate>();
					var serializationStore = configuration.Resolve<ITargetDataStore>();
					var sourceStore = configuration.Resolve<ISourceDataStore>();
					var dpConfig = configuration.Resolve<IUnicornDataProviderConfiguration>();

					var rootReference = serializationStore.GetByPathAndId(item.Path, item.Id, item.DatabaseName);
					if (rootReference != null)
					{
						logger.Warn("[D] existing serialized items under {0}".FormatWith(rootReference.GetDisplayIdentifier()));
						serializationStore.Remove(rootReference);
					}

					logger.Info("[U] Serializing included items under root {0}".FormatWith(item.GetDisplayIdentifier()));

					if (!predicate.Includes(item).IsIncluded) return false;

					DumpTreeInternal(item, predicate, serializationStore, sourceStore, logger, dpConfig);
				}
			}
			return true;
		}

		/// <returns>True if the item was dumped, false if it was not included</returns>
		public virtual bool DumpItem(IItemData item, IConfiguration[] configurations = null)
		{
			using (new TransparentSyncDisabler())
			{
				if (configurations == null) configurations = GetConfigurationsForItem(item);

				foreach(var configuration in configurations)
				{
					if (configuration == null) return false;

					var predicate = configuration.Resolve<IPredicate>();
					var serializationStore = configuration.Resolve<ITargetDataStore>();

					CacheManager.ClearAllCaches(); // BOOM! This clears all caches before we begin; 
												   // because for a TpSync configuration we could have TpSync items in the data cache which 'taint' the reserialize
												   // from being purely database

					var result = DumpItemInternal(item, predicate, serializationStore).IsIncluded;

					CacheManager.ClearAllCaches(); // BOOM! And we clear everything again at the end, because now
												   // for a TpSync configuration we might have DATABASE items in cache where we want TpSync.

					if (!result) return false;
				}

				return true;
			}
		}

		/// <remarks>All roots must live within the same configuration! Make sure that the roots are from the target data store.</remarks>
		public virtual bool SyncTree(IConfiguration configuration, Action<IItemData> rootLoadedCallback = null, bool runSyncStartPipeline = true, params IItemData[] roots)
		{
			var logger = configuration.Resolve<ILogger>();


			if (runSyncStartPipeline)
			{
				var startArgs = new UnicornSyncStartPipelineArgs(new []{ configuration }, logger);
				CorePipeline.Run("unicornSyncStart", startArgs);
			}

			var beginArgs = new UnicornSyncBeginPipelineArgs(configuration);
			CorePipeline.Run("unicornSyncBegin", beginArgs);

			if (beginArgs.Aborted)
			{
				logger.Error("Unicorn Sync Begin pipeline was aborted. Not executing sync for this configuration.");
				return false;
			}

			if (beginArgs.SyncIsHandled)
			{
				logger.Info("Unicorn Sync Begin pipeline signalled that it handled the sync for this configuration.");
				return true;
			}

			var syncStartTimestamp = DateTime.Now;

			using (new TransparentSyncDisabler())
			{
				var retryer = configuration.Resolve<IDeserializeFailureRetryer>();
				var consistencyChecker = configuration.Resolve<IConsistencyChecker>();
				var loader = configuration.Resolve<SerializationLoader>();

				if (roots.Length > 0)
				{
					loader.LoadAll(roots, retryer, consistencyChecker, rootLoadedCallback);
				}
				else
				{
					logger.Warn($"{configuration.Name} had no root paths included to sync. If you're only syncing roles, this is expected. Otherwise it indicates that your predicate has no included items and you need to add some.");
				}
			}

			CorePipeline.Run("unicornSyncComplete", new UnicornSyncCompletePipelineArgs(configuration, syncStartTimestamp));

			return true;
		}

		protected virtual void DumpTreeInternal(IItemData root, IPredicate predicate, ITargetDataStore serializationStore, ISourceDataStore sourceDataStore, ILogger logger, IUnicornDataProviderConfiguration dpConfig)
		{
			if (dpConfig.EnableTransparentSync)
			{
				CacheManager.ClearAllCaches(); 
				// BOOM! This clears all caches before we begin; 
				// because for a TpSync configuration we could have TpSync items in the data cache which 'taint' the reserialize
				// from being purely database
			}

			// we throw items into this queue, and let a thread pool pick up anything available to process in parallel. only the children of queued items are processed, not the item itself
			ConcurrentQueue<IItemData> processQueue = new ConcurrentQueue<IItemData>();

			// exceptions thrown on background threads are left in here
			ConcurrentQueue<Exception> errors = new ConcurrentQueue<Exception>();

			// we keep track of how many threads are actively processing something so we know when to end the threads
			// (e.g. a thread could have nothing in the queue right now, but that's because a different thread is about
			// to add 8 things to the queue - so it shouldn't quit till all is done)
			int activeThreads = 0;

			using (new UnicornOperationContext())
			{
				var rootResult = DumpItemInternal(root, predicate, serializationStore);
				if (!rootResult.IsIncluded) return;
			}

			processQueue.Enqueue(root);

			Thread[] pool = Enumerable.Range(0, ThreadCount).Select(i => new Thread(() =>
			{
				Process:
				Interlocked.Increment(ref activeThreads);
				IItemData parentItem;

				while (processQueue.TryDequeue(out parentItem) && errors.Count == 0)
				{
					using (new UnicornOperationContext()) // disablers only work on the current thread. So we need to disable on all worker threads
					{
						var children = sourceDataStore.GetChildren(parentItem);

						foreach (var item in children)
						{
							try
							{
								// we dump each item in the queue item
								// we do a whole array of children at a time because this makes the serialization of all children of a given item single threaded
								// this gives us a deterministic result of naming when name collisions occur, which means trees will not contain random differences
								// when reserialized (oh joy, that)
								var dump = DumpItemInternal(item, predicate, serializationStore);
								if (dump.IsIncluded)
								{
									// if the item is included, then we add its children as a queued work item
									processQueue.Enqueue(item);
								}
								else
								{
									logger.Warn("[S] {0} because {1}".FormatWith(item.GetDisplayIdentifier(), dump.Justification));
								}
							}
							catch (Exception ex)
							{
								errors.Enqueue(ex);
								break;
							}
						}
					}
				}

				// if we get here, the queue was empty. let's make ourselves inactive.
				Interlocked.Decrement(ref activeThreads);

				// if some other thread in our pool was doing stuff, sleep for a sec to see if we can pick up their work
				if (activeThreads > 0)
				{
					Thread.Sleep(10);
					goto Process; // OH MY GOD :)
				}
			})).ToArray();

			// start the thread pool
			foreach (var thread in pool) thread.Start();

			// ...and then wait for all the threads to finish
			foreach (var thread in pool) thread.Join();

			if (dpConfig.EnableTransparentSync)
			{
				CacheManager.ClearAllCaches(); 
				// BOOM! And we clear everything again at the end, because now
				// for a TpSync configuration we might have DATABASE items in cache where we want TpSync.
			}

			if (errors.Count > 0) throw new AggregateException(errors);
		}

		protected virtual PredicateResult DumpItemInternal(IItemData item, IPredicate predicate, ITargetDataStore targetDataStore)
		{
			var predicateResult = predicate.Includes(item);
			if (!predicateResult.IsIncluded) return predicateResult;

			targetDataStore.Save(item);
			return predicateResult;
		}
	}
}
