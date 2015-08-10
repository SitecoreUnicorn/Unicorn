using System;
using System.Linq;
using Rainbow.Model;
using Rainbow.Predicates;
using Sitecore.Pipelines;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Loader;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncBegin;
using Unicorn.Pipelines.UnicornSyncComplete;
using Unicorn.Predicates;

namespace Unicorn
{
	public class SerializationHelper
	{
		public virtual IConfiguration GetConfigurationForItem(IItemData item)
		{
			return UnicornConfigurationManager.Configurations.FirstOrDefault(configuration => configuration.Resolve<IPredicate>().Includes(item).IsIncluded);
		}

		/// <returns>True if the tree was dumped, false if the root item was not included</returns>
		public virtual bool DumpTree(IItemData item)
		{
			var configuration = GetConfigurationForItem(item);

			if (configuration == null) return false;

			var logger = configuration.Resolve<ILogger>();

			var predicate = configuration.Resolve<IPredicate>();
			var serializationStore = configuration.Resolve<ITargetDataStore>();
			var sourceStore = configuration.Resolve<ISourceDataStore>();

			var rootReference = serializationStore.GetByMetadata(item, item.DatabaseName);
			if (rootReference != null)
			{
				logger.Warn("[D] existing serialized items under {0}".FormatWith(rootReference.GetDisplayIdentifier()));
				serializationStore.Remove(rootReference);
			}

			logger.Info("[U] Serializing included items under root {0}".FormatWith(item.GetDisplayIdentifier()));

			if (!predicate.Includes(item).IsIncluded) return false;

			DumpTreeRecursive(item, predicate, serializationStore, sourceStore, logger);
			return true;
		}

		/// <returns>True if the item was dumped, false if it was not included</returns>
		public virtual bool DumpItem(IItemData item)
		{
			var configuration = GetConfigurationForItem(item);

			if (configuration == null) return false;

			var predicate = configuration.Resolve<IPredicate>();
			var serializationStore = configuration.Resolve<ITargetDataStore>();

			return DumpItemInternal(item, predicate, serializationStore).IsIncluded;
		}

		/// <remarks>All roots must live within the same configuration! Make sure that the roots are from the target data store.</remarks>
		public virtual bool SyncTree(IConfiguration configuration, Action<IItemData> rootLoadedCallback = null, params IItemData[] roots)
		{
			var logger = configuration.Resolve<ILogger>();

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
			
			var retryer = configuration.Resolve<IDeserializeFailureRetryer>();
			var consistencyChecker = configuration.Resolve<IConsistencyChecker>();
			var loader = configuration.Resolve<SerializationLoader>();

			loader.LoadAll(roots, retryer, consistencyChecker, rootLoadedCallback);

			CorePipeline.Run("unicornSyncComplete", new UnicornSyncCompletePipelineArgs(configuration, syncStartTimestamp));

			return true;
		}

		protected virtual void DumpTreeRecursive(IItemData root, IPredicate predicate, ITargetDataStore serializationStore, ISourceDataStore sourceDataStore, ILogger logger)
		{
			var dump = DumpItemInternal(root, predicate, serializationStore);
			if (dump.IsIncluded)
			{
				foreach (var child in sourceDataStore.GetChildren(root))
				{
					DumpTreeRecursive(child, predicate, serializationStore, sourceDataStore, logger);
				}
			}
			else
			{
				logger.Warn("[S] {0} because {1}".FormatWith(root.GetDisplayIdentifier(), dump.Justification));
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
