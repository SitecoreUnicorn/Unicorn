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
		/// Checks if any of the current predicate's root paths exist in the source provider
		/// </summary>
		public static bool AllRootPathsExist(IConfiguration configuration)
		{
			var predicate = configuration.Resolve<PredicateRootPathResolver>();
			var sourceDataStore = configuration.Resolve<ISourceDataStore>();

			return predicate.GetRootPaths().All(include => RootPathExists(sourceDataStore, include));
		}

		private static bool RootPathExists(IDataStore dataStore, TreeRoot include)
		{
			return dataStore.GetByPath(include.Path, include.DatabaseName).FirstOrDefault() != null;
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

			var resolver = new InterconfigurationDependencyResolver();

			return resolver.OrderByDependencies(targetConfigurations);
		}
	}
}
