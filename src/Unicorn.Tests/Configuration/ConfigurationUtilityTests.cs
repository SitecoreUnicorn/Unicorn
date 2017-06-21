using System;
using FluentAssertions;
using Unicorn.Configuration;
using Xunit;

namespace Unicorn.Tests.Configuration
{
	public class ConfigurationUtilityTests
	{
		[Fact]
		public void ResolveConfigurationPath_ResolvesExpectedPath_WhenPathIsAbsolute()
		{
			ConfigurationUtility.ResolveConfigurationPath("c:\\web").Should().Be("c:\\web");
		}

		[Fact]
		public void ResolveConfigurationPath_ResolvesExpectedPath_WhenPathIsRootRelative()
		{
			// HostingEnvironment returns null out of web context so path = empty string
			ConfigurationUtility.ResolveConfigurationPath("~/").Should().Be(AppDomain.CurrentDomain.BaseDirectory);
		}

		[Fact]
		public void ResolveConfigurationPath_ResolvesExpectedPath_WhenPathIsRelative()
		{
			// HostingEnvironment returns null out of web context so root = empty string
			ConfigurationUtility.ResolveConfigurationPath("~/../hello").Should().Be($"{AppDomain.CurrentDomain.BaseDirectory}\\..\\hello");
		}
	}
}
