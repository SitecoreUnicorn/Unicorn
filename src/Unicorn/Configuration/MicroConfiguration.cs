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

		public MicroConfiguration(string name, string description)
		{
			Name = name;
			Description = description;
		}

		public string Name { get; private set; }
		public string Description { get; private set; }

		/// <summary>
		/// Resolves a registered type with its dependencies. Note: to activate unregistered types and inject dependencies use Activate() instead.
		/// </summary>
		/// <typeparam name="T">Type to resolve</typeparam>
		/// <returns></returns>
		public virtual T Resolve<T>() where T : class
		{
			var typeOfT = typeof(T);

			var result = Resolve(typeOfT);

			if (result == null)
			{
				return (T)Activate(typeOfT, new KeyValuePair<string, object>[] { });
			}

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

		public virtual void Register(Type type, Func<object> factory, bool singleInstance)
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

		/// <summary>
		/// Creates an instance of an object, injecting any registered dependencies in the configuration into its constructor.
		/// </summary>
		/// <param name="type">Type to resolve</param>
		/// <param name="unmappedConstructorParameters">Constructor parameters that are not expected to be in the configuration</param>
		public virtual object Activate(Type type, KeyValuePair<string, object>[] unmappedConstructorParameters)
		{
			var constructors = type.GetConstructors();
			if (constructors.Length > 1) throw new MicroResolutionException("Cannot construct {0} because it has > 1 constructor.".FormatWith(type.FullName));
			if (constructors.Length == 0) throw new MicroResolutionException("Cannot construct {0} because it has no constructor!".FormatWith(type.FullName));

			var constructor = constructors.First();

			var ctorParams = constructor.GetParameters();

			object[] args = new object[ctorParams.Length];

			for (int parameterIndex = 0; parameterIndex < ctorParams.Length; parameterIndex++)
			{
				var currentParam = ctorParams[parameterIndex];

				if (unmappedConstructorParameters.Any(kv => kv.Key.Equals(currentParam.Name, StringComparison.OrdinalIgnoreCase)))
				{
					args[parameterIndex] = unmappedConstructorParameters.First(kv => kv.Key.Equals(currentParam.Name, StringComparison.OrdinalIgnoreCase)).Value;
				}
				else
				{
					args[parameterIndex] = Resolve(currentParam.ParameterType);
					if (args[parameterIndex] == null)
					{
						try
						{
							args[parameterIndex] = Activate(currentParam.ParameterType, new KeyValuePair<string, object>[] { });
						}
						catch (Exception ex)
						{
							throw new MicroResolutionException("Cannot activate {0}, constructor param '{1}' ({2}). The type '{2}' is probably not registered, or may need to be an explicit unmapped parameter (as an XML attribute on the type registration). Inner message: {3}".FormatWith(type.FullName, currentParam.Name, currentParam.ParameterType.Name, ex.Message));
						}
					}
				}
			}

			var lambdaParams = Expression.Parameter(typeof(object[]), "parameters");
			var newParams = new Expression[ctorParams.Length];

			for (int i = 0; i < ctorParams.Length; i++)
			{
				var paramsParameter = Expression.ArrayIndex(lambdaParams, Expression.Constant(i));

				newParams[i] = Expression.Convert(paramsParameter, ctorParams[i].ParameterType);
			}

			var newExpression = Expression.New(constructor, newParams);

			var constructionLambda = Expression.Lambda(typeof(Func<object[], object>), newExpression, lambdaParams);

			return ((Func<object[], object>)constructionLambda.Compile()).Invoke(args);
		}
	}
}
