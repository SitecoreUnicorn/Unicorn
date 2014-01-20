using System;
using Sitecore.Diagnostics;

namespace Unicorn.Dependencies
{
	/// <summary>
	/// This wrapper class prevents calling Register() on the Registry.Default dependency registry. This registry is global and immutable.
	/// </summary>
	internal class StaticDependencyRegistryWrapper : IDependencyRegistry
	{
		private readonly IDependencyRegistry _innerRegistry;

		public StaticDependencyRegistryWrapper(IDependencyRegistry innerRegistry)
		{
			Assert.ArgumentNotNull(innerRegistry, "innerRegistry");

			_innerRegistry = innerRegistry;
		}

		public T Resolve<T>() where T : class
		{
			return _innerRegistry.Resolve<T>();
		}

		public void Register<TType>(Func<TType> instanceFactory) where TType : class
		{
			throw new InvalidOperationException("You cannot register new dependencies on the default dependency registry. Register a different depdendency registry class in configuration instead, or if doing transient dependency injection use Registry.CreateCopyOfDefault() to create a mutable registry.");
		}
	}
}
