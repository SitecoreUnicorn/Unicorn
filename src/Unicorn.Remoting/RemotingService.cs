using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Engines;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.Remoting
{
	public class RemotingService
	{
		public RemotingPackage CreateRemotingPackage(string configurationName, DateTime ifModifiedSince)
		{
			Assert.ArgumentCondition(ifModifiedSince < DateTime.UtcNow, "ifModifiedSince", "ifModifiedSince was in the future. No no no.");

			var configuration = UnicornConfigurationManager.Configurations.FirstOrDefault(x => x.Name.Equals(configurationName, StringComparison.Ordinal));

			Assert.IsNotNull(configuration, "Invalid configuration specified.");

			var logger = configuration.Resolve<ILogger>();

			var package = new RemotingPackage(configuration);

			if (ifModifiedSince == Constants.NotSyncedDateTime || (DateTime.UtcNow - ifModifiedSince) > Factory.GetDatabase("master").Engines.HistoryEngine.Storage.EntryLifeTime)
			{
				// load using full sync strategy - either we have nothing local or the last sync was too long ago to rely on history engine
				logger.Info("Remoting full serialization: Processing Unicorn configuration " + configuration.Name);

				ProcessFullSyncPackage(package, configuration, logger);

				logger.Info("Remoting full serialization: Finished reserializing Unicorn configuration " + configuration.Name);
			}
			else
			{
				// load using history engine methodology (differential)

				logger.Info("Remoting history engine serialization: Processing Unicorn configuration " + configuration.Name);

				ProcessHistoryEnginePackage(package, configuration, ifModifiedSince);

				logger.Info("Remoting history engine serialization: Finished Unicorn configuration " + configuration.Name);
			}

			return package;
		}

		private void ProcessFullSyncPackage(RemotingPackage package, IConfiguration configuration, ILogger logger)
		{
			package.Manifest.Strategy = RemotingStrategy.Full;
			using (new SecurityDisabler())
			{
				var predicate = configuration.Resolve<IPredicate>();

				var roots = configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems();

				foreach (var root in roots)
				{
					logger.Info("[U] Serializing included items under root {0}".FormatWith(root.DisplayIdentifier));
					Serialize(root, predicate, package.SerializationProvider, logger);
				}
			}
		}

		private static void ProcessHistoryEnginePackage(RemotingPackage package, IConfiguration configuration, DateTime ifModifiedSince)
		{
			// TODO: need to "coalesce" history so we are not replaying more events than we need to
			// e.g. create then move in one sync will cause the created item to not be present as a serialized item
			// in the package because it was moved so the create fails

			package.Manifest.Strategy = RemotingStrategy.Differential;

			using (new SecurityDisabler())
			{
				var serializationProvider = package.SerializationProvider;

				var roots = configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems();

				var historyDatabases = roots.Select(x => x.DatabaseName).Distinct().Select(Factory.GetDatabase).ToArray();

				foreach (var historyDatabase in historyDatabases)
				{
					var localHistory = GetMergedHistory(ifModifiedSince, historyDatabase);

					foreach (var historyEntry in localHistory)
					{
						if (historyEntry.Action == HistoryAction.Moved)
						{
							var item = historyDatabase.GetItem(historyEntry.ItemId);

							if (item == null) continue; // invalid history entry - item deleted

							var manifestEntry = RemotingPackageManifestEntry.FromEntry(historyEntry, historyDatabase.Name);

							manifestEntry.OldItemPath = historyEntry.ItemPath; // on a moved entry, the itempath is the pre-move path
							manifestEntry.ItemPath = item.Paths.Path; // the path from the Item is the post-move path

							package.Manifest.AddEntry(manifestEntry);
						}
						else if (historyEntry.Action == HistoryAction.Deleted)
						{
							package.Manifest.AddEntry(RemotingPackageManifestEntry.FromEntry(historyEntry, historyDatabase.Name));
						}
						else
						{
							var item = historyDatabase.GetItem(historyEntry.ItemId);

							if (item == null) continue; // invalid history entry - item deleted

							// serialize updated item to package directory
							serializationProvider.SerializeItem(new SitecoreSourceItem(item));

							package.Manifest.AddEntry(RemotingPackageManifestEntry.FromEntry(historyEntry, historyDatabase.Name));
						}
					}
				}

				package.Manifest.LastSynchronized = DateTime.UtcNow;
			}
		}

		private static IEnumerable<HistoryEntry> GetMergedHistory(DateTime ifModifiedSince, Database historyDatabase)
		{
			var rawHistory = historyDatabase.Engines.HistoryEngine.GetHistory(ifModifiedSince, DateTime.UtcNow)
				.Where(x => x.Action != HistoryAction.Copied); // don't care - the newly copied items are create/save entries themselves

			var itemHistory = rawHistory.GroupBy(x => x.ItemId);

			var results = new List<HistoryEntry>();

			foreach (var group in itemHistory)
			{
				// if only one entry for this item ID, keep it and stop
				if (group.Count() == 1)
				{
					results.Add(group.First());
					continue;
				}

				// if there are no moves we should be fine with just the first action (e.g. create/save/save/save/save or create/save/addversion)
				if (group.All(x => x.Action != HistoryAction.Moved))
				{
					results.Add(group.First());
					continue;
				}

				// if there are moves - but no create, this means we can just send the move (which will have the latest serialized content)
				if (group.All(x => x.Action != HistoryAction.Created))
				{
					results.Add(group.First(x=>x.Action == HistoryAction.Moved));
				}

				// TODO missing cases
				throw new NotImplementedException("TODO: this should handle other cases, eg rename?");
			}

			return results;
		}

		private void Serialize(ISourceItem root, IPredicate predicate, ISerializationProvider serializationProvider, ILogger logger)
		{
			var predicateResult = predicate.Includes(root);
			if (predicateResult.IsIncluded)
			{
				serializationProvider.SerializeItem(root);

				foreach (var child in root.Children)
				{
					Serialize(child, predicate, serializationProvider, logger);
				}
			}
			else
			{
				logger.Warn("[S] {0} because {1}".FormatWith(root.DisplayIdentifier, predicateResult.Justification));
			}
		}
	}
}
