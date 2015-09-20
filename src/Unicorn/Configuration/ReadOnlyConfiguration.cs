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
		private readonly IConfiguration _innerConfiguration;

		public ReadOnlyConfiguration(IConfiguration innerConfiguration)
		{
			Assert.ArgumentNotNull(innerConfiguration, "innerConfiguration");

			_innerConfiguration = innerConfiguration;
		}

		public string Name { get { return _innerConfiguration.Name; }}
		public string Description { get { return _innerConfiguration.Description; } }

		public T Resolve<T>() where T : class
		{
			return _innerConfiguration.Resolve<T>();
		}

		public object Resolve(Type type)
		{
			return _innerConfiguration.Resolve(type);
		}

		public void Register(Type type, Func<object> factory, bool singleInstance)
		{
			throw new InvalidOperationException("You cannot register new dependencies on a read-only configuration.");
		}

		public object Activate(Type type, KeyValuePair<string, object>[] unmappedConstructorParameters)
		{
			return _innerConfiguration.Activate(type, unmappedConstructorParameters);
		}
	}
}
