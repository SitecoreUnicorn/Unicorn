using FluentAssertions;
using Unicorn.Data.DataProvider;
using Xunit;

namespace Unicorn.Tests.Data.DataProvider
{
	public class DataProviderConfigurationTests
	{
		[Fact]
		public void ShouldRespectConstructorParam()
		{
			var configuration = new DefaultUnicornDataProviderConfiguration(true);

			configuration.EnableTransparentSync.Should().BeTrue();
		}

		[Fact]
		public void ShouldRespectConstructorParam_WhenFalse()
		{
			var configuration = new DefaultUnicornDataProviderConfiguration(false);

			configuration.EnableTransparentSync.Should().BeFalse();
		}
	}
}