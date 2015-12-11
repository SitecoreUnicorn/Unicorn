using System;
using System.Collections.Generic;
using System.Linq;
using Unicorn.Data;
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

		public ConfigurationDependencyResolver(IConfiguration configuration)
		{
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
			if (DependencyCache.ContainsKey(_configuration))
				return DependencyCache[_configuration];

			var explicitDependencies = GetExplicitDependencies(_configuration);
			var dependencies = GetImplicitDependencies(_configuration).Union(explicitDependencies).ToArray();

			DependencyCache.Add(_configuration, dependencies);

			return dependencies;
		}

		/// <summary>
		/// Finds configurations that explicitly depend on the current configuration (e.g. declare a dependency)
		/// </summary>
		protected virtual IEnumerable<IConfigurationDependency> GetExplicitDependencies(IConfiguration configuration)
		{
			if (configuration.Dependencies == null)
				return new IConfigurationDependency[0];

			return AllConfigurations
				.Where(config => configuration.Dependencies.Any(dependency => dependency.Equals(config.Name, StringComparison.OrdinalIgnoreCase)))
				.Select(config => new ExplicitConfigurationDependency(config));
		}

		/// <summary>
		/// Finds configurations that implicitly depend on this one based on paths. 
		/// </summary>
		/// <remarks>
		/// Implicit dependency requires some explanation. This is a situation such as:
		/// - Config A includes /sitecore/content/Foo, without children
		/// - Config B includes /sitecore/content/Foo/Lol
		/// If and only if the /sitecore/content/Foo item does not yet exist Config B will gain an implicit dependency on Config A,
		/// such that Config B does not fail to load due to the missing parent contained in Config A.
		/// </remarks>
		protected virtual IEnumerable<IConfigurationDependency> GetImplicitDependencies(IConfiguration configuration)
		{
			var otherConfigurations = AllConfigurations
				.Where(config => !config.Name.Equals(configuration.Name, StringComparison.OrdinalIgnoreCase))
				.ToArray();

			var currentPredicate = configuration.Resolve<PredicateRootPathResolver>();
			var sourceDataStore = configuration.Resolve<ISourceDataStore>();
			var rootPaths = currentPredicate.GetRootPaths();

			foreach (var rootPath in rootPaths)
			{
				var parentPath = GetParentPath(rootPath.Path);

				// if the include was of /sitecore parent has no meaning, ignore it
				if (string.IsNullOrEmpty(parentPath)) continue;

				// If the parent is found in the source data store, then we do not need to check for implicit dependencies
				// (e.g. this config's load will not fail due to a missing parent item)
				var item = sourceDataStore.GetByPath(parentPath, rootPath.DatabaseName).FirstOrDefault();

				if (item != null) continue;

				// Look for parent in other configurations, if any exist
				foreach (var otherConfiguration in otherConfigurations)
				{
					// If the parent is found in a target data store, then we have a dependency
					var targetDataStore = otherConfiguration.Resolve<ITargetDataStore>();

					item = targetDataStore.GetByPath(parentPath, rootPath.DatabaseName).FirstOrDefault();

					if (item == null) continue;

					yield return new ImplicitConfigurationDependency(otherConfiguration, rootPath, parentPath);

					break;
				}
			}
		}

		protected static string GetParentPath(string rootPath)
		{
			var path = rootPath.TrimEnd('/');
			return path.Substring(0, path.LastIndexOf('/'));
		}
	}
}