using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sitecore.Diagnostics;
using Unicorn.Predicates;

namespace Unicorn.Configuration.Dependencies
{
	public class ConfigurationDependencyResolver
	{
		private static readonly IDictionary<IConfiguration, IConfigurationDependency[]> DependencyCache = new Dictionary<IConfiguration, IConfigurationDependency[]>();

		private IConfigurationDependency[] _dependencies;
		private IConfiguration[] _dependents;
		private IConfiguration[] _allConfigurations;
		private readonly IConfiguration _configuration;
		protected readonly object SyncLock = new object();

		public ConfigurationDependencyResolver(IConfiguration configuration)
		{
			Assert.ArgumentNotNull(configuration, nameof(configuration));

			_configuration = configuration;
		}

		public IConfigurationDependency[] Dependencies => _dependencies ?? (_dependencies = ResolveDependencies());
		public IConfiguration[] Dependents => _dependents ?? (_dependents = ResolveDependents());

		public IConfiguration[] AllConfigurations
		{
			get { return _allConfigurations ?? (_allConfigurations = UnicornConfigurationManager.Configurations); }
			set { _allConfigurations = value; }
		}

		/// <summary>
		/// Finds configurations that depend on the context configuration
		/// </summary>
		protected virtual IConfiguration[] ResolveDependents()
		{
			return AllConfigurations
				.Where(c => c.Resolve<ConfigurationDependencyResolver>().Dependencies.Any(d => d.Configuration.Name.Equals(_configuration.Name, StringComparison.OrdinalIgnoreCase)))
				.ToArray();
		}

		/// <summary>
		/// Finds configurations that the current configuration depends on
		/// </summary>
		private IConfigurationDependency[] ResolveDependencies()
		{
			lock (SyncLock)
			{
				IConfigurationDependency[] result;

				if (DependencyCache.TryGetValue(_configuration, out result)) return result;

				var dependencies = GetDependencies(_configuration);

				DependencyCache.Add(_configuration, dependencies);

				return dependencies;
			}
		}

		/// <summary>
		/// Finds configurations that explicitly depend on the current configuration (e.g. declare a dependency)
		/// </summary>
		protected virtual IConfigurationDependency[] GetDependencies(IConfiguration configuration)
		{
			if (configuration.Dependencies == null)
				return new IConfigurationDependency[0];

			return GetExplicitDependencies(configuration).Concat(GetImplicitDependencies(configuration)).ToArray();
		}

		protected virtual IEnumerable<IConfigurationDependency> GetExplicitDependencies(IConfiguration configuration)
		{
			return AllConfigurations
				.Where(config => configuration.Dependencies.Any(dependency => IsWildcardMatch(config.Name, dependency)))
				.Select(config => (IConfigurationDependency) new ExplicitConfigurationDependency(config));
		}

		protected virtual IEnumerable<IConfigurationDependency> GetImplicitDependencies(IConfiguration configuration)
		{
			var configRootPaths = configuration.Resolve<IPredicate>().GetRootPaths();

			var nonIgnoredConfigurations = AllConfigurations
				.Where(config => configuration.IgnoredImplicitDependencies
					.All(ignoredDep => !IsWildcardMatch(config.Name, ignoredDep)
				));

			foreach (var config in nonIgnoredConfigurations)
			{
				if (config.Name.Equals(configuration.Name, StringComparison.OrdinalIgnoreCase)) continue; // don't depend on yourself :)

				var candidateParentPaths = config.Resolve<IPredicate>().GetRootPaths();

				bool match = false;

				foreach (var candidateParent in candidateParentPaths)
				{
					foreach (var configRoot in configRootPaths)
					{
						// mismatching dbs = don't care about path
						if (!configRoot.DatabaseName.Equals(candidateParent.DatabaseName)) continue;

						var configRootPath = $"{configRoot.Path.TrimEnd('/')}/";
						var candidateParentPath = $"{candidateParent.Path.TrimEnd('/')}/";
						if (configRootPath.StartsWith(candidateParentPath) && !configRootPath.Equals(candidateParentPath, StringComparison.Ordinal))
						{
							match = true;
							break;
						}
					}

					if (match) break;
				}

				if (match) yield return new ImplicitConfigurationDependency(config);
			}
		}

		/// <summary>
		/// Checks if a string matches a wildcard argument (using regex)
		/// </summary>
		protected static bool IsWildcardMatch(string input, string wildcards)
		{

			return Regex.IsMatch(input, "^" + Regex.Escape(wildcards).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
		}
	}
}