using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.Caching;
using Sitecore.Pipelines;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Data.Dilithium;
using Unicorn.Loader;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornOperationStart;
using Unicorn.Pipelines.UnicornSyncBegin;
using Unicorn.Pipelines.UnicornSyncComplete;
using Unicorn.Predicates;
// ReSharper disable TooWideLocalVariableScope

namespace Unicorn
{
	/// <summary>
	/// Utility class designed to make it a bit simpler to call primary serialization operations programmatically
	/// </summary>
	public class SerializationHelper
	{
		public virtual IConfiguration[] GetConfigurationsForItem(IItemData item)
		{
			return UnicornConfigurationManager.Configurations.Where(configuration => configuration.Resolve<IPredicate>().Includes(item).IsIncluded).ToArray();
		}

		/// <returns>True if the tree was dumped, false if the root item was not included</returns>
		public virtual bool DumpTree(IItemData item, bool runReserializeStartPipeline, IConfiguration[] configurations = null)
		{
			using (new TransparentSyncDisabler())
			{
				if (configurations == null) configurations = GetConfigurationsForItem(item);

				// check if Dilithium was already running. If it was, we won't dispose it when we're done.
				bool dilithiumWasStarted = ReactorContext.Reactor != null;

				if (runReserializeStartPipeline)
				{
					var startArgs = new UnicornOperationStartPipelineArgs(configurations, configurations.First().Resolve<ILogger>());
					CorePipeline.Run("unicornReserializeStart", startArgs);
				}

				try
				{
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
				finally
				{
					if(!dilithiumWasStarted) ReactorContext.Dispose();
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

				foreach (var configuration in configurations)
				{
					if (configuration == null) return false;

					var predicate = configuration.Resolve<IPredicate>();
					var serializationStore = configuration.Resolve<ITargetDataStore>();
					var dpConfig = configuration.Resolve<IUnicornDataProviderConfiguration>();

					if (dpConfig.EnableTransparentSync)
					{
						CacheManager.ClearAllCaches(); 
						// BOOM! This clears all caches before we begin; 
						// because for a TpSync configuration we could have TpSync items in the data cache which 'taint' the reserialize
						// from being purely database
					}

					var result = DumpItemInternal(item, predicate, serializationStore).IsIncluded;

					if (dpConfig.EnableTransparentSync)
					{
						CacheManager.ClearAllCaches(); 
						// BOOM! And we clear everything again at the end, because now
						// for a TpSync configuration we might have DATABASE items in cache where we want TpSync.
					}

					if (!result) return false;
				}

				return true;
			}
		}

		/// <remarks>All roots must live within the same configuration! Make sure that the roots are from the target data store.</remarks>
		public virtual bool SyncTree(IConfiguration configuration, Action<IItemData> rootLoadedCallback = null, bool runSyncStartPipeline = true, params IItemData[] roots)
		{
			var logger = configuration.Resolve<ILogger>();

			// check if Dilithium was already running. If it was, we won't dispose it when we're done.
			bool dilithiumWasStarted = ReactorContext.Reactor != null;

			if (runSyncStartPipeline)
			{
				var startArgs = new UnicornOperationStartPipelineArgs(new[] { configuration }, logger);
				CorePipeline.Run("unicornSyncStart", startArgs);
			}

			var beginArgs = new UnicornSyncBeginPipelineArgs(configuration);
			CorePipeline.Run("unicornSyncBegin", beginArgs);

			if (beginArgs.Aborted)
			{
				if (!dilithiumWasStarted) ReactorContext.Dispose();

				logger.Error("Unicorn Sync Begin pipeline was aborted. Not executing sync for this configuration.");
				return false;
			}

			if (beginArgs.SyncIsHandled)
			{
				if (!dilithiumWasStarted) ReactorContext.Dispose();

				logger.Info("Unicorn Sync Begin pipeline signalled that it handled the sync for this configuration.");
				return true;
			}

			var syncStartTimestamp = DateTime.Now;

			try
			{
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
			}
			catch
			{
				ReactorContext.Dispose();
				throw;
			}

			if (!dilithiumWasStarted) ReactorContext.Dispose();

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
			var processQueue = new Queue<IItemData>();

			using (new UnicornOperationContext())
			{
				var rootResult = DumpItemInternal(root, predicate, serializationStore);
				if (!rootResult.IsIncluded) return;
			}

			processQueue.Enqueue(root);

			IItemData parentItem;

			while (processQueue.Count > 0)
			{
				parentItem = processQueue.Dequeue();

				using (new UnicornOperationContext()) // disablers only work on the current thread. So we need to disable on all worker threads
				{
					var children = sourceDataStore.GetChildren(parentItem);

					foreach (var item in children)
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
				}
			}

			if (dpConfig.EnableTransparentSync)
			{
				CacheManager.ClearAllCaches();
				// BOOM! And we clear everything again at the end, because now
				// for a TpSync configuration we might have DATABASE items in cache where we want TpSync.
			}
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
