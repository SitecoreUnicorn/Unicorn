using System.Collections.Generic;
using System.Linq;
using Rainbow.Storage;
using Unicorn.Configuration;
using Unicorn.Configuration.Dependencies;
using Unicorn.Data;
using Unicorn.Predicates;

namespace Unicorn.ControlPanel
{
	public static class ControlPanelUtility
	{
		/// <summary>
		/// Checks if any of the current predicate's root paths exist in the serialization provider
		/// </summary>
		public static bool HasAnySerializedItems(IConfiguration configuration)
		{
			var pathResolver = configuration.Resolve<PredicateRootPathResolver>();

			// if you have no root paths at all that's actually cool. You might just be serializing roles.
			// either way there are no missing root items to worry about, since there are no roots.
			if (pathResolver.GetRootPaths().Length == 0) return true;

			return pathResolver.GetRootSerializedItems().Length > 0;
		}

		/// <summary>
		/// Verifies that the parents of all root paths defined in the predicate exist in Sitecore
		/// In other words, that if one were to sync this configuration and the root items had to be added,
		/// would their parents exist?
		/// </summary>
		public static bool AllRootParentPathsExist(IConfiguration configuration)
		{
			var predicate = configuration.Resolve<PredicateRootPathResolver>();
			var sourceDataStore = configuration.Resolve<ISourceDataStore>();

			return predicate.GetRootPaths().All(include => RootPathParentExists(sourceDataStore, include));
		}

		/// <summary>
		/// Verifies that all root paths defined in the predicate exist in Sitecore
		/// In other words, if you were to reserialize this configuration would there be something
		/// to serialize at all root locations?
		/// </summary>
		public static bool AllRootPathsExist(IConfiguration configuration)
		{
			var predicate = configuration.Resolve<PredicateRootPathResolver>();
			var sourceDataStore = configuration.Resolve<ISourceDataStore>();

			return predicate.GetRootPaths().All(include => RootPathExists(sourceDataStore, include));
		}

		/// <summary>
		/// Resolves a set of configurations matches from a caret-delimited query string value.
		/// Dependency order is considered in the returned order of configurations.
		/// </summary>
		public static IConfiguration[] ResolveConfigurationsFromQueryParameter(string queryParameter)
		{
			// parse query string value
			var configNames = (queryParameter ?? string.Empty)
				.Split('^')
				.Where(key => !string.IsNullOrWhiteSpace(key))
				.ToList();

			var allConfigurations = UnicornConfigurationManager.Configurations;

			// determine which configurations the query string resolves to
			IEnumerable<IConfiguration> selectedConfigurations;

			if (configNames.Count == 0)
			{
				// query string specified no configs. This means sync all.
				// but we still have to set in dependency order.
				selectedConfigurations = allConfigurations;
			}
			else
			{
				selectedConfigurations = configNames
					.Select(name => allConfigurations.FirstOrDefault(conf => conf.Name.Equals(name)))
					.Where(conf => conf != null);
			}

			// order the selected configurations in dependency order
			var resolver = new InterconfigurationDependencyResolver();

			return resolver.OrderByDependencies(selectedConfigurations);
		}

		private static bool RootPathParentExists(IDataStore dataStore, TreeRoot include)
		{
			if (include.Path.IndexOf('/') < 0) return false;

			var rootPathParent = include.Path.TrimEnd('/');

			rootPathParent = rootPathParent.Substring(0, rootPathParent.LastIndexOf('/'));

			if (rootPathParent.Equals(string.Empty)) rootPathParent = "/sitecore";

			return dataStore.GetByPath(rootPathParent, include.DatabaseName).FirstOrDefault() != null;
		}

		private static bool RootPathExists(IDataStore dataStore, TreeRoot include)
		{
			return dataStore.GetByPath(include.Path, include.DatabaseName).FirstOrDefault() != null;
		}
	}
}
