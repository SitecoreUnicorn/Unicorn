using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Syntax;

namespace Unicorn.Dependencies
{
	/// <summary>
	/// Defines a registered set of Unicorn dependencies using Ninject to lookup the dependencies
	/// </summary>
	public class NinjectConfiguration : IConfiguration
	{
		protected readonly IKernel Container = new StandardKernel();

		public NinjectConfiguration(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }

		/// <summary>
		/// Resolves a requested type, either directly registered or via constructor injection
		/// </summary>
		public T Resolve<T>()
			where T : class
		{
			return Container.Get<T>();
		}

		/// <summary>
		/// Registers a dependency that is constructed again each time it is requested using a factory delegate method. Will overwrite any existing dependency for this type.
		/// </summary>
		/// <typeparam name="TType">The dependency type being registered (interface or abstract class)</typeparam>
		/// <param name="instanceFactory">Method invoked to construct the dependency when requested</param>
		public void Register<TType>(Func<TType> instanceFactory) where TType : class
		{
			Container.Bind<TType>().ToMethod(ctx => instanceFactory()).InSingletonScope();
		}

		public void Register(Type type, Type implementation, KeyValuePair<string, object>[] unmappedConstructorParameters)
		{
			IBindingWithSyntax<object> bind = Container.Bind(type).To(implementation).InSingletonScope();

			if (unmappedConstructorParameters == null) return;

			foreach (var parameter in unmappedConstructorParameters)
			{
				bind = bind.WithConstructorArgument(parameter.Key, parameter.Value);
			}
		}
	}
}
