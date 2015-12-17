using System;
using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Configuration.Dependencies
{
	public class InterconfigurationDependencyResolver
	{
		protected int MaxIterations = 20000;

		public virtual IConfiguration[] OrderByDependencies(IEnumerable<IConfiguration> configurations)
		{
			var processQueue = new Queue<ConfigItem>(configurations.Select(config => new ConfigItem(config, configurations)));

			var added = new HashSet<string>();
			List<IConfiguration> result = new List<IConfiguration>();
			int iterationCount = 0;

			while (processQueue.Count > 0 && iterationCount < MaxIterations)
			{
				iterationCount++;

				var current = processQueue.Dequeue();

				// not all dependencies of the current item are added. Push it back onto the queue, and we'll pick it up later.
				if (!current.Dependencies.All(dep => added.Contains(dep.Name)))
				{
					processQueue.Enqueue(current);
					continue;
				}

				result.Add(current.Config);
				added.Add(current.Config.Name);
			}

			if (iterationCount == MaxIterations) throw new InvalidOperationException("There is a dependency loop in your Unicorn configuration dependencies. Unresolved configurations: " + string.Join(", ", processQueue.Select(x => x.Config.Name)));

			return result.ToArray();
		}

		private class ConfigItem
		{
			public ConfigItem(IConfiguration config, IEnumerable<IConfiguration> possibleConfigurations)
			{
				Config = config;
				Dependencies = config.Resolve<ConfigurationDependencyResolver>()
					.Dependencies
					.Select(dep => dep.Configuration)
					.Where(dep => possibleConfigurations.Any(conf => conf.Name.Equals(dep.Name)))
					.ToArray();
			}

			public IConfiguration Config { get; }
			public IConfiguration[] Dependencies { get; }
		}
	}
}
