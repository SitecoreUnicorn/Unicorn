using System;
using System.IO;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
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
		public void GetItems(string configurationName, DateTime ifModifiedSince, HttpResponseBase httpResponse, Stream stream)
		{
			Assert.ArgumentCondition(ifModifiedSince < DateTime.UtcNow, "ifModifiedSince", "ifModifiedSince was in the future. No no no.");

			var configuration = UnicornConfigurationManager.Configurations.FirstOrDefault(x => x.Name.Equals(configurationName, StringComparison.Ordinal));

			Assert.IsNotNull(configuration, "Invalid configuration specified.");

			var logger = configuration.Resolve<ILogger>();

			var package = new RemotingPackage(configuration);

			if (ifModifiedSince == Constants.NotSyncedDateTime || (DateTime.UtcNow - ifModifiedSince) > Factory.GetDatabase("master").Engines.HistoryEngine.Storage.EntryLifeTime)
			{
				// load using full sync methodology
				// return HTTP 200
				// return zip file of full sync dump
				httpResponse.StatusCode = 200;

				logger.Info("Remoting full serialization: Processing Unicorn configuration " + configuration.Name);

				ProcessFullSyncPackage(package, configuration, logger);

				logger.Info("Remoting full serialization: Finished reserializing Unicorn configuration " + configuration.Name);
			}
			else
			{
				// load using history engine methodology
				// return HTTP 203 partial information
				// return zip file with changed serialized items, and manifest.json with history details
				httpResponse.StatusCode = 203;

				logger.Info("Remoting history engine serialization: Processing Unicorn configuration " + configuration.Name);

				ProcessHistoryEnginePackage(package, configuration, ifModifiedSince);

				logger.Info("Remoting history engine serialization: Finished Unicorn configuration " + configuration.Name);
			}

			//package.WriteToHttpResponse(httpResponse);
			package.WriteToStream(stream);
		}

		private void ProcessFullSyncPackage(RemotingPackage package, IConfiguration configuration, ILogger logger)
		{
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
			using (new SecurityDisabler())
			{
				var serializationProvider = package.SerializationProvider;

				var roots = configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems();

				var historyDatabases = roots.Select(x => x.DatabaseName).Distinct().Select(Factory.GetDatabase).ToArray();

				foreach (var historyDatabase in historyDatabases)
				{
					var localHistory = historyDatabase.Engines.HistoryEngine.GetHistory(ifModifiedSince, DateTime.UtcNow);

					foreach (var historyEntry in localHistory)
					{
						if (historyEntry.Action == HistoryAction.Copied) continue; // don't care - the newly copied items are create/save entries themselves

						if (historyEntry.Action == HistoryAction.Moved)
						{
							var item = historyDatabase.GetItem(historyEntry.ItemId);

							if (item == null) continue; // invalid history entry - item deleted

							var manifestEntry = RemotingPackageManifestEntry.FromEntry(historyEntry);

							manifestEntry.OldItemPath = historyEntry.ItemPath; // on a moved entry, the itempath is the pre-move path
							manifestEntry.ItemPath = item.Paths.Path; // the path from the Item is the post-move path

							package.Manifest.AddEntry(manifestEntry);
						}
						else if (historyEntry.Action == HistoryAction.Deleted)
						{
							package.Manifest.AddEntry(RemotingPackageManifestEntry.FromEntry(historyEntry));
						}
						else
						{
							var item = historyDatabase.GetItem(historyEntry.ItemId);

							if (item == null) continue; // invalid history entry - item deleted

							// serialize updated item to package directory
							serializationProvider.SerializeItem(new SitecoreSourceItem(item));

							package.Manifest.AddEntry(RemotingPackageManifestEntry.FromEntry(historyEntry));
						}
					}
				}

				package.Manifest.LastSynchronized = DateTime.UtcNow;
			}
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
