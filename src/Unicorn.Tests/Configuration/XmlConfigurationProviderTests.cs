using Unicorn.Loader;
using Xunit;

namespace Unicorn.Tests.Configuration
{
	public class XmlConfigurationProviderTests
	{
		[Fact]
		public void ShouldLoadExpectedConfigurations()
		{
			var testProvider = new TestXmlConfigurationProvider();
			
			Assert.NotEmpty(testProvider.Configurations);
			Assert.Equal("Default Configuration", testProvider.Configurations[0].Name);
			Assert.Equal("Test Configuration", testProvider.Configurations[1].Name);
		}

		[Fact]
		public void ShouldResolveExpectedDefaultDependency()
		{
			var testProvider = new TestXmlConfigurationProvider();

			Assert.IsType(typeof(DefaultSerializationLoaderLogger), testProvider.Configurations[0].Resolve<ISerializationLoaderLogger>());
		}

		[Fact]
		public void ShouldResolveExpectedOverriddenDependency()
		{
			var testProvider = new TestXmlConfigurationProvider();

			Assert.IsType(typeof(DebugSerializationLoaderLogger), testProvider.Configurations[1].Resolve<ISerializationLoaderLogger>());
		}
	}
}
