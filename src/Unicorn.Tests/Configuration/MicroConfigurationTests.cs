using System;
using Xunit;
using Unicorn.Configuration;

namespace Unicorn.Tests.Configuration
{
	public class MicroConfigurationTests
	{
		[Fact]
		public void ResolvesType()
		{
			var micro = new MicroConfiguration("Test", null);

			micro.Register(typeof(ITest), () => new Test(), true);

			var instance = micro.Resolve<ITest>();

			Assert.NotNull(instance);
		}

		[Fact]
		public void Throws_WhenConstructingUnregisteredType()
		{
			var micro = new MicroConfiguration("Test", null);

			Assert.Throws<MicroResolutionException>(() => micro.Resolve<ITest>());
		}

		[Fact]
		public void ResolvesType_AsInstance()
		{
			var micro = new MicroConfiguration("Test", null);

			micro.Register(typeof(IInstance), () => new TestInstance(), false);
			
			var instance = micro.Resolve<IInstance>();

			Assert.NotNull(instance);

			var instance2 = micro.Resolve<IInstance>();

			Assert.NotEqual(instance.InstanceGuid, instance2.InstanceGuid);
		}

		[Fact]
		public void ResolvesType_AsSingleton()
		{
			var micro = new MicroConfiguration("Test", null);

			micro.Register(typeof(IInstance), () => new TestInstance(), true);
			
			var instance = micro.Resolve<IInstance>();

			Assert.NotNull(instance);

			var instance2 = micro.Resolve<IInstance>();

			Assert.Equal(instance.InstanceGuid, instance2.InstanceGuid);
		}

		public interface IInstance
		{
			Guid InstanceGuid { get; }
		}

		public class TestInstance : IInstance
		{
			public TestInstance()
			{
				InstanceGuid = Guid.NewGuid();
			}

			public Guid InstanceGuid { get; private set; }
		}
	}
}
