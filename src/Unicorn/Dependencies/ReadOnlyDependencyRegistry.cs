using System;
using System.Collections.Generic;
using Sitecore.Diagnostics;

namespace Unicorn.Dependencies
{
	/// <summary>
	/// This wrapper class prevents performing dependency reconfiguration on configurations after the setup phase is complete.
	/// </summary>
	internal class ReadOnlyDependencyRegistry : IConfiguration
	{
		private readonly IConfiguration _innerRegistry;

		public ReadOnlyDependencyRegistry(IConfiguration innerRegistry)
		{
			Assert.ArgumentNotNull(innerRegistry, "innerRegistry");

			_innerRegistry = innerRegistry;
		}

		public string Name { get { return _innerRegistry.Name; }}

		public T Resolve<T>() where T : class
		{
			return _innerRegistry.Resolve<T>();
		}

		public void Register<TType>(Func<TType> instanceFactory) where TType : class
		{
			throw new InvalidOperationException("You cannot register new dependencies on a read-only dependency registry.");
		}

		public void Register(Type type, Type implementation, KeyValuePair<string, object>[] unmappedConstructorParameters)
		{
			throw new InvalidOperationException("You cannot register new dependencies on a read-only dependency registry.");
		}
	}
}
