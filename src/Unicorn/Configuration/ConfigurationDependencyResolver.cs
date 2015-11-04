using System;
using System.Collections.Generic;
using System.Linq;
using Unicorn.Predicates;

namespace Unicorn.Configuration
{
    public class ConfigurationDependencyResolver
    {
        private static readonly IDictionary<IConfiguration, IConfigurationDependency[]> _dependencyCache = new Dictionary<IConfiguration, IConfigurationDependency[]>();

        private IConfigurationDependency[] _dependencies;
        private IConfiguration[] _dependants;
        public IConfiguration Configuration { get; set; }

        public ConfigurationDependencyResolver(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfigurationDependency[] Dependencies => _dependencies ?? (_dependencies = ResolveDependencies());
        public IConfiguration[] Dependants => _dependants ?? (_dependants = ResolveDependants());

        private IConfiguration[] ResolveDependants()
        {
            return UnicornConfigurationManager.Configurations.Where(c => c.Resolve<ConfigurationDependencyResolver>().Dependencies.Any(d => d.Configuration.Name.Equals(Configuration.Name, StringComparison.CurrentCultureIgnoreCase))).ToArray();
        }

        private IConfigurationDependency[] ResolveDependencies()
        {
            if (_dependencyCache.ContainsKey(Configuration))
                return _dependencyCache[Configuration];
            var explicitDependencies = GetExplicitDependencies();
            var dependencies = GetRootDependencies().Union(explicitDependencies).ToArray();
            _dependencyCache.Add(Configuration, dependencies);
            return dependencies;
        }

        private IEnumerable<IConfigurationDependency> GetExplicitDependencies()
        {
            if (Configuration.Dependencies == null)
                return new IConfigurationDependency[] {};

            var dependencies = UnicornConfigurationManager.Configurations.Where(c => Configuration.Dependencies.Any(d => d.Equals(c.Name, StringComparison.InvariantCultureIgnoreCase)));
            return dependencies.Select(c => (IConfigurationDependency)new ExplicitConfigurationDependency(c));
        }

        private IEnumerable<IConfigurationDependency> GetRootDependencies()
        {
            var otherConfigurations = UnicornConfigurationManager.Configurations.Where(c => c.Name != Configuration.Name).ToArray();

            var currentPredicate = Configuration.Resolve<PredicateRootPathResolver>();
            foreach (var rootPath in currentPredicate.GetRootPaths())
            {
                var parentPath = GetParentPath(rootPath.Path);

                //If the parent is found in the source data store, then there is not dependency on this
                var item = currentPredicate.SourceDataStore.GetByPath(parentPath, rootPath.DatabaseName).FirstOrDefault();
                if (item != null)
                    continue;

                //Look for parent in other configurations
                foreach (var otherConfiguration in otherConfigurations)
                {
                    //If the parent is found in a target data store, then we have a dependency
                    var dataStore = otherConfiguration.Resolve<PredicateRootPathResolver>().TargetDataStore;
                    item = dataStore.GetByPath(parentPath, rootPath.DatabaseName).FirstOrDefault();
                    if (item == null)
                        continue;
                    yield return new ItemConfigurationDependency(otherConfiguration, rootPath, parentPath);
                    break;
                }
            }
        }

        private static string GetParentPath(string rootPath)
        {
            var path = rootPath.TrimEnd('/');
            return path.Substring(0, path.LastIndexOf('/'));
        }
    }
}