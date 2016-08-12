using System;
using System.Linq;
using NSubstitute;
using Rainbow.Storage;
using Unicorn.Configuration;
using Unicorn.Configuration.Dependencies;
using Unicorn.Predicates;

namespace Unicorn.Tests.Configuration.Dependencies
{
	static class DepTestHelper
	{
		public static void GroomConfigs(IConfiguration[] configurations)
		{
			foreach (var config in configurations)
			{
				config.Resolve<ConfigurationDependencyResolver>().AllConfigurations = configurations;
			}
		}

		public static IConfiguration CreateTestConfiguration(string name, params string[] dependencies)
		{
			var config = Substitute.For<IConfiguration>();

			config.Name.Returns(name);
			config.Dependencies.Returns(dependencies);
			config.Resolve<ConfigurationDependencyResolver>().Returns(new ConfigurationDependencyResolver(config));

			return config;
		}

		public static IConfiguration CreateImplicitTestConfiguration(string name, params Tuple<string, string>[] includedDbsAndPaths)
		{
			var config = Substitute.For<IConfiguration>();

			var fakePredicate = Substitute.For<IPredicate>();
			fakePredicate.GetRootPaths().Returns(info => includedDbsAndPaths.Select(include => new TreeRoot("Fakety Fake", include.Item2, include.Item1)).ToArray());

			config.Name.Returns(name);
			config.Resolve<IPredicate>().Returns(fakePredicate);
			config.Resolve<ConfigurationDependencyResolver>().Returns(new ConfigurationDependencyResolver(config));

			return config;
		}
	}
}
