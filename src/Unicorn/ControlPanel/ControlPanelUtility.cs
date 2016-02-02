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
			return configuration.Resolve<PredicateRootPathResolver>().GetRootSerializedItems().Length > 0;
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
			var config = (queryParameter ?? string.Empty)
				.Split('^')
				.Where(key => !string.IsNullOrWhiteSpace(key))
				.ToList();

			var configurations = UnicornConfigurationManager.Configurations;
			if (config.Count == 0) return configurations;

			var targetConfigurations = config.Select(name => configurations.FirstOrDefault(conf => conf.Name.Equals(name)))
				.Where(conf => conf != null)
				.ToArray();

			var resolver = new InterconfigurationDependencyResolver();

			return resolver.OrderByDependencies(targetConfigurations);
		}

		private static bool RootPathParentExists(IDataStore dataStore, TreeRoot include)
		{
			if (include.Path.IndexOf('/') < 0) return false;

			var rootPathParent = include.Path.TrimEnd('/');

			rootPathParent = rootPathParent.Substring(0, rootPathParent.LastIndexOf('/'));

			if (rootPathParent.Equals("/")) rootPathParent = "/sitecore";

			return dataStore.GetByPath(rootPathParent, include.DatabaseName).FirstOrDefault() != null;
		}

		private static bool RootPathExists(IDataStore dataStore, TreeRoot include)
		{
			return dataStore.GetByPath(include.Path, include.DatabaseName).FirstOrDefault() != null;
		}
	}
}
