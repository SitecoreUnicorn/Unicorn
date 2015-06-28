using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sitecore.StringExtensions;

namespace Unicorn.Configuration
{
	public class XmlConfigurationTypeActivator
	{
		private readonly IConfiguration _configuration;

		public XmlConfigurationTypeActivator(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		/// <summary>
		/// Creates an instance of an object, injecting any registered dependencies in the configuration into its constructor.
		/// </summary>
		/// <param name="type">Type to resolve</param>
		/// <param name="unmappedConstructorParameters">Constructor parameters that are not expected to be in the configuration</param>
		public virtual object Activate(Type type, KeyValuePair<string, object>[] unmappedConstructorParameters)
		{
			var constructors = type.GetConstructors();
			if (constructors.Length > 1) throw new MicroResolutionException("Cannot construct types with > 1 constructor using Micro.");

			var constructor = constructors.First();

			var ctorParams = constructor.GetParameters();

			object[] args = new object[ctorParams.Length];

			for (int parameterIndex = 0; parameterIndex < ctorParams.Length; parameterIndex++)
			{
				var currentParam = ctorParams[parameterIndex];

				if (unmappedConstructorParameters.Any(kv => kv.Key.Equals(currentParam.Name, StringComparison.Ordinal)))
				{
					args[parameterIndex] = unmappedConstructorParameters.First(kv => kv.Key.Equals(currentParam.Name, StringComparison.Ordinal)).Value;
				}
				else
				{
					args[parameterIndex] = _configuration.Resolve(currentParam.ParameterType);
					if (args[parameterIndex] == null) throw new MicroResolutionException("Cannot activate {0} because dependency {1} had no mapping.".FormatWith(type.FullName, currentParam.ParameterType));
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
