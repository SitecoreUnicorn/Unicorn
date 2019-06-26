using FluentAssertions;
using Unicorn.Configuration.Dependencies;
using Xunit;

namespace Unicorn.Tests.Configuration.Dependencies
{
	public partial class ConfigurationDependencyResolverTests
	{
		[Fact]
		public void ShouldResolveNoDependencies()
		{
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("A")
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[0].Resolve<ConfigurationDependencyResolver>();

			sut.Dependencies.Length.Should().Be(0);
			sut.Dependents.Length.Should().Be(0);
		}

		[Fact]
		public void ShouldResolveExplicitDependencies()
		{
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("A"),
				DepTestHelper.CreateTestConfiguration("B", "A")
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[1].Resolve<ConfigurationDependencyResolver>();

			sut.Dependencies.Length.Should().Be(1);
			sut.Dependencies[0].Configuration.Name.Should().Be("A");
		}

		[Fact]
		public void ShouldResolveExplicitWildcardDependencies()
		{
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("Alpha"),
				DepTestHelper.CreateTestConfiguration("Alpha One"),
				DepTestHelper.CreateTestConfiguration("B", "Alpha*")
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[2].Resolve<ConfigurationDependencyResolver>();

			sut.Dependencies.Length.Should().Be(2);
			sut.Dependencies.Should().Contain(dep => dep.Configuration.Name.Equals("Alpha"));
			sut.Dependencies.Should().Contain(dep => dep.Configuration.Name.Equals("Alpha One"));
		}

		[Fact]
		public void ShouldResolveExplicitDependents()
		{
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("A"),
				DepTestHelper.CreateTestConfiguration("B", "A")
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[0].Resolve<ConfigurationDependencyResolver>();

			sut.Dependents.Length.Should().Be(1);
			sut.Dependents[0].Name.Should().Be("B");
		}
	}
}
