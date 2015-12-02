using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
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
		/// Checks if any of the current predicate's root paths exist in the source provider
		/// </summary>
		public static bool AllRootPathsExists(IConfiguration configuration)
		{
			var predicate = configuration.Resolve<PredicateRootPathResolver>();
			return predicate.GetRootPaths().All(include => RootPathsExists(predicate.SourceDataStore, include));
		}

		private static bool RootPathsExists(IDataStore dataStore, TreeRoot include)
		{
			if (dataStore.GetByPath(include.Path, include.DatabaseName).FirstOrDefault() != null)
				return true;
			return ParentPathExists(dataStore, include);
		}

		private static bool ParentPathExists(IDataStore dataStore, TreeRoot include)
		{
			var path = include.Path.TrimEnd('/');
			var parentPath = path.Substring(0, path.LastIndexOf('/'));
			return dataStore.GetByPath(parentPath, include.DatabaseName).FirstOrDefault() != null;
		}

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

			return targetConfigurations;
		}

		public static IEnumerable<IConfigurationDependency> FindConfigurationsDependencies(IConfiguration configuration)
		{
			return configuration.Resolve<ConfigurationDependencyResolver>().Dependencies;
		}

		public static IEnumerable<IConfiguration> FindConfigurationsDependents(IConfiguration configuration)
		{
			return configuration.Resolve<ConfigurationDependencyResolver>().Dependents;
		}

		public static bool HasDependents(IConfiguration configuration)
		{
			return configuration.Resolve<ConfigurationDependencyResolver>().Dependents.Any();
		}
	}
}
