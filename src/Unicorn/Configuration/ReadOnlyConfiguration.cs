using System;
using System.Collections.Generic;
using Sitecore.Diagnostics;

namespace Unicorn.Configuration
{
	/// <summary>
	/// This wrapper class prevents performing dependency reconfiguration on configurations after the setup phase is complete.
	/// </summary>
	internal class ReadOnlyConfiguration : IConfiguration
	{
		private readonly IConfiguration _innerRegistry;

		public ReadOnlyConfiguration(IConfiguration innerRegistry)
		{
			Assert.ArgumentNotNull(innerRegistry, "innerRegistry");

			_innerRegistry = innerRegistry;
		}

		public string Name { get { return _innerRegistry.Name; }}

		public T Resolve<T>() where T : class
		{
			return _innerRegistry.Resolve<T>();
		}

		public object Resolve(Type type)
		{
			return _innerRegistry.Resolve(type);
		}

		public void Register(Type type, Func<object> factory, bool singleInstance)
		{
			throw new InvalidOperationException("You cannot register new dependencies on a read-only configuration.");
		}

		public object Activate(Type type, KeyValuePair<string, object>[] unmappedConstructorParameters)
		{
			return _innerRegistry.Activate(type, unmappedConstructorParameters);
		}
	}
}
