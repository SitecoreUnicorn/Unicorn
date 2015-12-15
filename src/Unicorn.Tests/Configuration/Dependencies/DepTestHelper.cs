using NSubstitute;
using Unicorn.Configuration;
using Unicorn.Configuration.Dependencies;

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
	}
}
