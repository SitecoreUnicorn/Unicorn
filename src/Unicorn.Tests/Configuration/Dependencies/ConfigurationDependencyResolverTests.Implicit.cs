using System;
using FluentAssertions;
using Unicorn.Configuration.Dependencies;
using Xunit;

namespace Unicorn.Tests.Configuration.Dependencies
{
	public partial class ConfigurationDependencyResolverTests
	{
		[Fact]
		public void IdenticalPaths_ShouldNotBeImplicitDependencies()
		{
			var configs = new[]
			{
				DepTestHelper.CreateImplicitTestConfiguration("A", Tuple.Create("master", "/sitecore/content")),
				DepTestHelper.CreateImplicitTestConfiguration("B", Tuple.Create("master", "/sitecore/content"))
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[1].Resolve<ConfigurationDependencyResolver>();

			sut.Dependencies.Should().BeEmpty();
		}

		[Fact]
		public void ShouldResolveImplicitDependencies()
		{
			var configs = new[]
			{
				DepTestHelper.CreateImplicitTestConfiguration("A", Tuple.Create("master", "/sitecore/content")),
				DepTestHelper.CreateImplicitTestConfiguration("B", Tuple.Create("master", "/sitecore/content/Home"))
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[1].Resolve<ConfigurationDependencyResolver>();

			sut.Dependencies.Length.Should().Be(1);
			sut.Dependencies[0].Configuration.Name.Should().Be("A");
		}

		[Fact]
		public void ShouldResolveImplicitDependencies_WithSimilarPathRoots()
		{
			var configs = new[]
			{
				DepTestHelper.CreateImplicitTestConfiguration("A", Tuple.Create("master", "/sitecore/content")),
				DepTestHelper.CreateImplicitTestConfiguration("B", Tuple.Create("master", "/sitecore/contented/Home"))
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[1].Resolve<ConfigurationDependencyResolver>();

			sut.Dependencies.Should().BeEmpty();
		}

		[Fact]
		public void ShouldResolveImplicitDependencies_WithMultiple()
		{
			var configs = new[]
			{
				DepTestHelper.CreateImplicitTestConfiguration("A", Tuple.Create("master", "/sitecore/content")),
				DepTestHelper.CreateImplicitTestConfiguration("B", Tuple.Create("master", "/sitecore/content/Home")),
				DepTestHelper.CreateImplicitTestConfiguration("C", Tuple.Create("master", "/sitecore/content/Home/Funky")),
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[2].Resolve<ConfigurationDependencyResolver>();

			sut.Dependencies.Length.Should().Be(2);
			sut.Dependencies[0].Configuration.Name.Should().Be("A");
			sut.Dependencies[1].Configuration.Name.Should().Be("B");
		}

		[Fact]
		public void ShouldResolveImplicitDependents()
		{
			var configs = new[]
			{
				DepTestHelper.CreateImplicitTestConfiguration("A", Tuple.Create("master", "/sitecore/content")),
				DepTestHelper.CreateImplicitTestConfiguration("B", Tuple.Create("master", "/sitecore/content/Home"))
			};

			DepTestHelper.GroomConfigs(configs);

			var sut = configs[0].Resolve<ConfigurationDependencyResolver>();

			sut.Dependents.Length.Should().Be(1);
			sut.Dependents[0].Name.Should().Be("B");
		}
	}
}
