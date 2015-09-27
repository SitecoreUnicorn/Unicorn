using System.Linq;
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
		public static bool HasAnySourceItems(IConfiguration configuration)
		{
			return configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems().Length > 0;
		}

		public static IConfiguration[] ResolveConfigurationsFromQueryParameter(string queryParameter)
		{
			var config = (queryParameter ?? string.Empty)
				.Split('^')
				.Where(key => !string.IsNullOrWhiteSpace(key))
				.ToLookup(key => key);

			var configurations = UnicornConfigurationManager.Configurations;
			if (config.Count == 0) return configurations;

			var targetConfigurations = configurations.Where(x => config.Contains(x.Name)).ToArray();

			return targetConfigurations;
		}
	}
}
