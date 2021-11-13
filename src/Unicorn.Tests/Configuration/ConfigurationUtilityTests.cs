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
			// some test runners (e.g. Resharper) already have the trailing '\\' on BaseDirectory
			var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			if (!baseDirectory.EndsWith("\\"))
			{
				baseDirectory += "\\";
			}

			// HostingEnvironment returns null out of web context so root = empty string
			ConfigurationUtility.ResolveConfigurationPath("~/../hello").Should().Be($"{baseDirectory}..\\hello"); 
		}
	}
}
