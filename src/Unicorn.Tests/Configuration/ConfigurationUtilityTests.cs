using Unicorn.Configuration;
using Xunit;

namespace Unicorn.Tests.Configuration
{
	public class ConfigurationUtilityTests
	{
		[Fact]
		public void ResolveConfigurationPath_ResolvesExpectedPath_WhenPathIsAbsolute()
		{
			Assert.Equal("c:\\web", ConfigurationUtility.ResolveConfigurationPath("c:\\web"));
		}

		[Fact]
		public void ResolveConfigurationPath_ResolvesExpectedPath_WhenPathIsRootRelative()
		{
			// HostingEnvironment returns null out of web context so path = empty string
			Assert.Equal("", ConfigurationUtility.ResolveConfigurationPath("~/"));
		}

		[Fact]
		public void ResolveConfigurationPath_ResolvesExpectedPath_WhenPathIsRelative()
		{
			// HostingEnvironment returns null out of web context so root = empty string
			Assert.Equal("..\\hello", ConfigurationUtility.ResolveConfigurationPath("~/../hello"));
		}
	}
}
