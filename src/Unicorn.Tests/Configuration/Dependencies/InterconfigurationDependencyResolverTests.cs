using System;
using FluentAssertions;
using NSubstitute;
using Unicorn.Configuration.Dependencies;
using Xunit;

namespace Unicorn.Tests.Configuration.Dependencies
{
	public class InterconfigurationDependencyResolverTests
	{
		[Fact]
		public void ShouldResolveWithoutDependencies()
		{
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("C"),
				DepTestHelper.CreateTestConfiguration("B"),
				DepTestHelper.CreateTestConfiguration("A")
			};
			
			var sut = new InterconfigurationDependencyResolver();

			DepTestHelper.GroomConfigs(configs);

			var result = sut.OrderByDependencies(configs);

			result.Length.Should().Be(3);
			result[0].Name.Should().Be("C");
			result[1].Name.Should().Be("B");
			result[2].Name.Should().Be("A");
		}

		[Fact]
		public void ShouldResolveWithDependencies()
		{
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("B", "A"),
				DepTestHelper.CreateTestConfiguration("A")
			};

			var sut = new InterconfigurationDependencyResolver();

			DepTestHelper.GroomConfigs(configs);

			var result = sut.OrderByDependencies(configs);

			result.Length.Should().Be(2);
			result[0].Name.Should().Be("A");
			result[1].Name.Should().Be("B");
		}

		[Fact]
		public void ShouldResolveWithTransitiveDependencies()
		{
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("C", "B"),
				DepTestHelper.CreateTestConfiguration("B", "A"),
				DepTestHelper.CreateTestConfiguration("A")
			};

			var sut = new InterconfigurationDependencyResolver();

			DepTestHelper.GroomConfigs(configs);

			var result = sut.OrderByDependencies(configs);

			result.Length.Should().Be(3);
			result[0].Name.Should().Be("A");
			result[1].Name.Should().Be("B");
			result[2].Name.Should().Be("C");
		}

		[Fact]
		public void ShouldPreserveInputOrderWithDependencies()
		{
			// expected order = items without deps first, followed by items with dependencies ordered in dependency order
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("A", "C"),
				DepTestHelper.CreateTestConfiguration("B", "A"),
				DepTestHelper.CreateTestConfiguration("C"),
				DepTestHelper.CreateTestConfiguration("D")
			};

			var sut = new InterconfigurationDependencyResolver();

			DepTestHelper.GroomConfigs(configs);

			var result = sut.OrderByDependencies(configs);

			result.Length.Should().Be(4);
			result[0].Name.Should().Be("C");
			result[1].Name.Should().Be("D");
			result[2].Name.Should().Be("A");
			result[3].Name.Should().Be("B");
		}

		[Fact]
		public void ShouldResolveMultipleDependencies()
		{
			// expected order = items without deps first, followed by items with dependencies ordered in dependency order
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("A", "B", "C"),
				DepTestHelper.CreateTestConfiguration("B", "D"),
				DepTestHelper.CreateTestConfiguration("C"),
				DepTestHelper.CreateTestConfiguration("D")
			};

			var sut = new InterconfigurationDependencyResolver();

			DepTestHelper.GroomConfigs(configs);

			var result = sut.OrderByDependencies(configs);

			result.Length.Should().Be(4);
			result[0].Name.Should().Be("C");
			result[1].Name.Should().Be("D");
			result[2].Name.Should().Be("B");
			result[3].Name.Should().Be("A");
		}

		[Fact]
		public void ShouldThrowWhenTransitiveRecursiveDependencyExists()
		{
			// expected order = items without deps first, followed by items with dependencies ordered in dependency order
			var configs = new[]
			{
				DepTestHelper.CreateTestConfiguration("A", "B"),
				DepTestHelper.CreateTestConfiguration("B", "C"),
				DepTestHelper.CreateTestConfiguration("C", "A")
			};

			var sut = new InterconfigurationDependencyResolver();

			DepTestHelper.GroomConfigs(configs);

			Assert.Throws<InvalidOperationException>(() => sut.OrderByDependencies(configs));
		}
	}
}
