using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Kamsar.WebConsole;
using Rainbow.Model;
using Sitecore.Caching;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.ControlPanel;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Data.Dilithium;
using Unicorn.Loader;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornReserializeComplete;
using Unicorn.Pipelines.UnicornReserializeStart;
using Unicorn.Pipelines.UnicornSyncBegin;
using Unicorn.Pipelines.UnicornSyncComplete;
using Unicorn.Pipelines.UnicornSyncEnd;
using Unicorn.Pipelines.UnicornSyncStart;
using Unicorn.Predicates;
// ReSharper disable TooWideLocalVariableScope

namespace Unicorn
{
	/// <summary>
	/// Utility class designed to make it a bit simpler to call primary serialization operations programmatically
	/// </summary>
	public class SerializationHelper
	{
		/// <summary>
		/// Case-insensitive dictionary for which the key-value pairs will be injected into the pipeline arguments as 'CustomData'.
		/// </summary>
		public IDictionary<string, object> PipelineArgumentData { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		public virtual IConfiguration[] GetConfigurationsForItem(IItemData item)
		{
			return UnicornConfigurationManager.Configurations.Where(configuration => configuration.Resolve<IPredicate>().Includes(item).IsIncluded).ToArray();
		}

		public virtual bool ReserializeConfigurations(IConfiguration[] configurations, IProgressStatus progress, ILogger additionalLogger)
		{
			int taskNumber = 1;

			try
			{
				var startArgs = new UnicornReserializeStartPipelineArgs(OperationType.FullReserialize, configurations, additionalLogger, null);
				MergePipelineArgs(startArgs);
				CorePipeline.Run("unicornReserializeStart", startArgs);

				foreach (var configuration in configurations)
				{
					var logger = configuration.Resolve<ILogger>();

					using (new LoggingContext(additionalLogger, configuration))
					{
						try
						{
							var timer = new Stopwatch();
							timer.Start();

							logger.Info(string.Empty);
							logger.Info(configuration.Name + " is being reserialized.");

							using (new TransparentSyncDisabler())
							{
								var targetDataStore = configuration.Resolve<ITargetDataStore>();
								var helper = configuration.Resolve<SerializationHelper>();

								// nuke any existing items in the store before we begin. This is a full reserialize so we want to
								// get rid of any existing stuff even if it's not part of existing configs
								logger.Warn("[D] Clearing existing items from {0} (if any)".FormatWith(targetDataStore.FriendlyName));
								targetDataStore.Clear();

								var roots = configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems();

								int index = 1;
								foreach (var root in roots)
								{
									helper.ReserializeTree(root, false, new[] { configuration });
									WebConsoleUtility.SetTaskProgress(progress, taskNumber, configurations.Length, (int)((index / (double)roots.Length) * 100));
									index++;
								}
							}

							timer.Stop();

							CorePipeline.Run("unicornReserializeComplete", MergePipelineArgs(new UnicornReserializeCompletePipelineArgs(configuration)));

							logger.Info("{0} reserialization complete in {1}ms.".FormatWith(configuration.Name, timer.ElapsedMilliseconds));
						}
						catch (Exception ex)
						{
							logger.Error(ex);
							return false;
						}

						taskNumber++;
					}
				}
			}
			finally
			{
				ReactorContext.Dispose();
			}

			return true;
		}

		/// <summary>
		/// Reserializes an item and all of its children.
		/// </summary>
		/// <param name="item">Root item to reserialize</param>
		/// <param name="runReserializeStartPipeline">Whether this is a standalone reserialize (true) or part of a larger batch (false)</param>
		/// <param name="configurations">The configuration(s) to allow reserialization of</param>
		/// <returns>True if the tree was dumped, false if the root item was not included</returns>
		public virtual bool ReserializeTree(IItemData item, bool runReserializeStartPipeline = true, IConfiguration[] configurations = null)
		{
			using (new TransparentSyncDisabler())
			{
				if (configurations == null) configurations = GetConfigurationsForItem(item);

				if (configurations.Length == 0) return false;

				// check if Dilithium was already running. If it was, we won't dispose it when we're done.
				bool dilithiumWasStarted = ReactorContext.IsActive;

				try
				{
					if (runReserializeStartPipeline)
					{
						var startArgs = new UnicornReserializeStartPipelineArgs(OperationType.PartialReserializeTree, configurations, configurations.First().Resolve<ILogger>(), item);
						MergePipelineArgs(startArgs);
						CorePipeline.Run("unicornReserializeStart", startArgs);
					}

					foreach (var configuration in configurations)
					{
						if (configuration == null) continue;

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

						logger.Info("[A] Serializing included items under root {0}".FormatWith(item.GetDisplayIdentifier()));

						if (!predicate.Includes(item).IsIncluded) continue;

						DumpTreeInternal(item, predicate, serializationStore, sourceStore, logger, dpConfig);
					}
				}
				finally
				{
					if (!dilithiumWasStarted) ReactorContext.Dispose();
				}
			}

			return true;
		}

		/// <returns>True if the item was dumped, false if it was not included</returns>
		public virtual bool ReserializeItem(IItemData item, IConfiguration[] configurations = null)
		{
			using (new TransparentSyncDisabler())
			{
				if (configurations == null) configurations = GetConfigurationsForItem(item);

				// return false; item not part of any configs
				if (configurations.Length == 0) return false;

				// check if Dilithium was already running. If it was, we won't dispose it when we're done.
				bool dilithiumWasStarted = ReactorContext.IsActive;

				try
				{
					var startArgs = new UnicornReserializeStartPipelineArgs(OperationType.PartialReserializeItem, configurations, configurations.First().Resolve<ILogger>(), item);
					MergePipelineArgs(startArgs);
					CorePipeline.Run("unicornReserializeStart", startArgs);

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
				}
				finally
				{
					if (!dilithiumWasStarted) ReactorContext.Dispose();
				}

				return true;
			}
		}

		public virtual bool SyncConfigurations(IConfiguration[] configurations, IProgressStatus progress, ILogger additionalLogger)
		{
			int taskNumber = 1;

			bool success = true;

			try
			{
				var startArgs = new UnicornSyncStartPipelineArgs(OperationType.FullSync, configurations, additionalLogger, null);
				MergePipelineArgs(startArgs);
				CorePipeline.Run("unicornSyncStart", startArgs);

				if (startArgs.SyncIsHandled)
				{
					additionalLogger.Info("Unicorn Sync Start pipeline signalled that it handled the sync for all configurations.");
					return true;
				}

				foreach (var configuration in configurations)
				{
					var logger = configuration.Resolve<ILogger>();
					var helper = configuration.Resolve<SerializationHelper>();

					using (new LoggingContext(additionalLogger, configuration))
					{
						try
						{
							var startStatement = new StringBuilder();
							startStatement.Append(configuration.Name);
							startStatement.Append(" is being synced");

							if (configuration.EnablesDilithium())
							{
								startStatement.Append(" with Dilithium");
								if (configuration.EnablesDilithiumSql()) startStatement.Append(" SQL");
								if (configuration.EnablesDilithiumSql() && configuration.EnablesDilithiumSfs()) startStatement.Append(" +");
								if (configuration.EnablesDilithiumSfs()) startStatement.Append(" Serialized");
								startStatement.Append(" enabled.");
							}
							else
							{
								startStatement.Append(".");
							}

							logger.Info(startStatement.ToString());

							using (new TransparentSyncDisabler())
							{
								var predicate = configuration.Resolve<IPredicate>();

								var roots = predicate.GetRootPaths();

								var index = 0;
								helper.SyncTree(
									configuration: configuration,
									rootLoadedCallback: item =>
									{
										WebConsoleUtility.SetTaskProgress(progress, taskNumber, configurations.Length, (int)((index / (double)roots.Length) * 100));
										index++;
									},
									runSyncStartPipeline: false
								);
							}
						}
						catch (DeserializationSoftFailureAggregateException ex)
						{
							logger.Error(ex);
							// allow execution to continue, because the exception was non-fatal
						}
						catch (Exception ex)
						{
							logger.Error(ex);
							success = false;
							break;
						}
					}

					taskNumber++;
				}
			}
			finally
			{
				ReactorContext.Dispose();
			}

			try
			{
				CorePipeline.Run("unicornSyncEnd", MergePipelineArgs(new UnicornSyncEndPipelineArgs(additionalLogger, success, configurations)));
			}
			catch (Exception exception)
			{
				Log.Error("Error occurred in unicornSyncEnd pipeline.", exception);
				additionalLogger.Error(exception);
			}

			return success;
		}

		/// <remarks>All roots must live within the same configuration! Make sure that the roots are from the target data store.</remarks>
		public virtual bool SyncTree(IConfiguration configuration, Action<IItemData> rootLoadedCallback = null, bool runSyncStartPipeline = true, IItemData partialSyncRoot = null)
		{
			var logger = configuration.Resolve<ILogger>();

			// check if Dilithium was already running. If it was, we won't dispose it when we're done.
			bool dilithiumWasStarted = ReactorContext.IsActive;

			var syncStartTimestamp = DateTime.Now;

			try
			{
				if (runSyncStartPipeline)
				{
					var startArgs = new UnicornSyncStartPipelineArgs(OperationType.PartialSync, new[] { configuration }, logger, partialSyncRoot);
					MergePipelineArgs(startArgs);
					CorePipeline.Run("unicornSyncStart", startArgs);

					if (startArgs.SyncIsHandled)
					{
						logger.Info("Unicorn Sync Start pipeline signalled that it handled the sync for all configurations.");
						return true;
					}
				}

				var beginArgs = new UnicornSyncBeginPipelineArgs(configuration);
				MergePipelineArgs(beginArgs);
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

				using (new TransparentSyncDisabler())
				{
					var retryer = configuration.Resolve<IDeserializeFailureRetryer>();
					var consistencyChecker = configuration.Resolve<IConsistencyChecker>();
					var loader = configuration.Resolve<SerializationLoader>();

					IItemData[] roots;

					if (partialSyncRoot != null) roots = new[] { partialSyncRoot };
					else
					{
						var pathResolver = configuration.Resolve<PredicateRootPathResolver>();

						roots = pathResolver.GetRootSerializedItems();
					}

					if (roots.Length > 0)
					{
						loader.LoadAll(roots, retryer, consistencyChecker, rootLoadedCallback);
					}
					else
					{
						logger.Warn($"{configuration.Name} had no root paths included to sync. If you're only syncing roles and/or users, this is expected. Otherwise it indicates that your predicate has no included items and you need to add some.");
					}
				}
			}
			catch (DeserializationSoftFailureAggregateException)
			{
				RunUnicornSyncComplete(configuration, syncStartTimestamp);
				throw;
			}
			finally
			{
				if (!dilithiumWasStarted) ReactorContext.Dispose();
			}

			RunUnicornSyncComplete(configuration, syncStartTimestamp);

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

		protected virtual T MergePipelineArgs<T>(T args)
			where T : PipelineArgs
		{
			foreach (var pair in PipelineArgumentData)
			{
				object value;
				if (!args.CustomData.TryGetValue(pair.Key, out value))
					args.CustomData[pair.Key] = pair.Value;
			}

			return args;
		}

		private void RunUnicornSyncComplete(IConfiguration configuration, DateTime syncStartTimestamp)
		{
			CorePipeline.Run("unicornSyncComplete", MergePipelineArgs(new UnicornSyncCompletePipelineArgs(configuration, syncStartTimestamp)));
		}
	}
}
