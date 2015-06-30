using System.Collections.Generic;
using NUnit.Framework;
using Unicorn.Configuration;

namespace Unicorn.Tests.Configuration
{
	public class XmlConfigurationTypeActivatorTests
	{
		[Test]
		public void Activator_ResolvesType_WithDependency()
		{
			var micro = new MicroConfiguration("Test");

			micro.Register(typeof(ITest), () => new Test(), true);

			var instance = (IDependency)micro.Activate(typeof(TestDependencyParameter), new KeyValuePair<string, object>[] { });

			Assert.IsNotNull(instance);

			Assert.IsNotNull(instance.TestInstance);
		}

		[Test]
		public void Activator_ResolvesType_WithDependency_AndParameter()
		{
			var micro = new MicroConfiguration("Test");

			micro.Register(typeof(ITest), () => new Test(), true);

			var instance = (IDependency)micro.Activate(typeof(TestDependencyParameterStatic), new[] { new KeyValuePair<string, object>("value", "hello") });

			Assert.IsNotNull(instance);

			Assert.AreEqual("hello", ((TestDependencyParameterStatic)instance).Value);
		}
	}

	public interface ITest
	{
		string TestString { get; }
	}

	public class Test : ITest
	{
		public string TestString { get; set; }
	}

	public interface IDependency
	{
		ITest TestInstance { get; }
	}

	public class TestDependencyParameter : IDependency
	{
		public TestDependencyParameter(ITest dependency)
		{
			TestInstance = dependency;
		}

		public ITest TestInstance { get; private set; }
	}

	public class TestDependencyParameterStatic : IDependency
	{
		public TestDependencyParameterStatic(ITest depdendency, string value)
		{
			TestInstance = depdendency;
			Value = value;
		}

		public ITest TestInstance { get; private set; }
		public string Value { get; set; }
	}
}
