using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sitecore.StringExtensions;

namespace Unicorn.Configuration
{
	public class MicroConfiguration : IConfiguration
	{
		private readonly ConcurrentDictionary<Type, Lazy<object>> _singletons = new ConcurrentDictionary<Type, Lazy<object>>();
		private readonly ConcurrentDictionary<Type, Func<object>> _transients = new ConcurrentDictionary<Type, Func<object>>();

		public MicroConfiguration(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		public virtual T Resolve<T>() where T : class
		{
			var typeOfT = typeof(T);

			var result = Resolve(typeOfT);

			if (result == null) throw new MicroResolutionException("Nothing registered for ".FormatWith(typeOfT.FullName));

			return (T)result;
		}

		public virtual object Resolve(Type type)
		{
			Lazy<object> value;
			if (_singletons.TryGetValue(type, out value))
			{
				return value.Value;
			}

			Func<object> factory;
			if (_transients.TryGetValue(type, out factory))
			{
				return factory();
			}

			return null;
		}

		public void Register(Type type, Func<object> factory, bool singleInstance)
		{
			if (singleInstance)
			{
				_singletons.TryAdd(type, new Lazy<object>(factory));
			}
			else
			{
				_transients.TryAdd(type, factory);
			}
		}	
	}
}
