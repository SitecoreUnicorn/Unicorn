using System;
using System.Collections.Generic;
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

		public void Register(Type type, Type implementation, bool singleInstance, KeyValuePair<string, object>[] unmappedConstructorParameters)
		{
			IBindingWithSyntax<object> bind = Container.Bind(type).To(implementation);

			if (singleInstance)
				bind = ((IBindingInSyntax<object>) bind).InSingletonScope();
			else
				bind = ((IBindingInSyntax<object>) bind).InTransientScope();

			if (unmappedConstructorParameters == null) return;

			foreach (var parameter in unmappedConstructorParameters)
			{
				bind = bind.WithConstructorArgument(parameter.Key, parameter.Value);
			}
		}
	}
}
