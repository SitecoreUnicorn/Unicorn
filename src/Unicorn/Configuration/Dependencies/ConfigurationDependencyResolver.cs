using System;
using System.Collections.Generic;
using System.Linq;

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

			var dependencies = GetDependencies(_configuration);

			DependencyCache.Add(_configuration, dependencies);

			return dependencies;
		}

		/// <summary>
		/// Finds configurations that explicitly depend on the current configuration (e.g. declare a dependency)
		/// </summary>
		protected virtual IConfigurationDependency[] GetDependencies(IConfiguration configuration)
		{
			if (configuration.Dependencies == null)
				return new IConfigurationDependency[0];

			return AllConfigurations
				.Where(config => configuration.Dependencies.Any(dependency => dependency.Equals(config.Name, StringComparison.OrdinalIgnoreCase)))
				.Select(config => (IConfigurationDependency)new ExplicitConfigurationDependency(config))
				.ToArray();
		}
	}
}