using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
		/// Gets any paths in a configuration's predicate that do not exist in Sitecore
		/// In other words, if you were to reserialize this configuration would there be something
		/// to serialize at all root locations?
		/// </summary>
		public static string[] GetInvalidRootPaths(IConfiguration configuration)
		{
			var predicate = configuration.Resolve<PredicateRootPathResolver>();
			var sourceDataStore = configuration.Resolve<ISourceDataStore>();

			return predicate.GetRootPaths()
				.Where(include => !RootPathExists(sourceDataStore, include))
				.Select(treeRoot => treeRoot.DatabaseName + ":" + treeRoot.Path)
				.ToArray(); 
		}

		/// <summary>
		/// Resolves a set of configurations matches from a caret-delimited query string value.
		/// Dependency order is considered in the returned order of configurations.
		/// </summary>
		public static IConfiguration[] ResolveConfigurationsFromQueryParameter(string queryParameter, string excludeParameter = null)
		{
			// parse query string value
			var configNames = (queryParameter ?? string.Empty)
				.Split('^')
				.Where(key => !string.IsNullOrWhiteSpace(key))
				.ToList();

			// parse exclude value
			var excludedConfigNames = (excludeParameter ?? string.Empty)
				.Split('^')
				.Where(key => !string.IsNullOrWhiteSpace(key))
				.ToList();

			var allConfigurations = UnicornConfigurationManager.Configurations;

			// determine which configurations the query string resolves to
			List<IConfiguration> selectedConfigurations = new List<IConfiguration>();

			if (configNames.Count == 0)
			{
				// query string specified no configs. This means sync all.
				// but we still have to set in dependency order.
				selectedConfigurations = allConfigurations.ToList();
			}
			else
			{
				// I can't LINQ like Kam can LINQ >.< ;-)
				// querystring specified configs, but each config name can be a wildcard pattern. We have to loop through
				// them all, and find any and all configs that match
				foreach (string configName in configNames)
				{
					var matchingConfigurations = allConfigurations.Where(c => WildcardMatch(c.Name, configName));
					foreach (var matchingConfiguration in matchingConfigurations)
					{
						if (!selectedConfigurations.Contains(matchingConfiguration))
							selectedConfigurations.Add(matchingConfiguration);
					}
				}
			}

			// process config excludes
			foreach (var exclude in excludedConfigNames)
			{
				selectedConfigurations.RemoveAll(c => WildcardMatch(c.Name, exclude));
			}

			// order the selected configurations in dependency order
			var resolver = new InterconfigurationDependencyResolver();

			return resolver.OrderByDependencies(selectedConfigurations);
		}

		// Should probably move this and all tasks of configuration matching, into the UnicornConfigurationManager at some point
		internal static bool WildcardMatch(string stringToMatch, string wildcard)
		{
			if (wildcard.Contains('?') || wildcard.Contains('*'))
			{
				string regexExpression = $"^{Regex.Escape(wildcard).Replace("\\?", ".").Replace("\\*", ".*")}$";
				return Regex.IsMatch(stringToMatch, regexExpression);
			}

			return stringToMatch.Equals(wildcard);
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
