using System;
using NUnit.Framework;
using Unicorn.Configuration;

namespace Unicorn.Tests.Configuration
{
	public class MicroConfigurationTests
	{
		[Test]
		public void Micro_ResolvesType()
		{
			var micro = new MicroConfiguration("Test");

			micro.Register(typeof(ITest), () => new Test(), true);

			var instance = micro.Resolve<ITest>();

			Assert.IsNotNull(instance);
		}

		[Test]
		public void Micro_ResolvesType_AsInstance()
		{
			var micro = new MicroConfiguration("Test");

			micro.Register(typeof(IInstance), () => new TestInstance(), false);
			
			var instance = micro.Resolve<IInstance>();

			Assert.IsNotNull(instance);

			var instance2 = micro.Resolve<IInstance>();

			Assert.AreNotEqual(instance.InstanceGuid, instance2.InstanceGuid);
		}

		[Test]
		public void Micro_ResolvesType_AsSingleton()
		{
			var micro = new MicroConfiguration("Test");

			micro.Register(typeof(IInstance), () => new TestInstance(), true);
			
			var instance = micro.Resolve<IInstance>();

			Assert.IsNotNull(instance);

			var instance2 = micro.Resolve<IInstance>();

			Assert.AreEqual(instance.InstanceGuid, instance2.InstanceGuid);
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
