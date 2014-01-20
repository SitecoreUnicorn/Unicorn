using System;

namespace Unicorn.Dependencies
{
	public interface IDependencyRegistry
	{
		/// <summary>
		/// Resolves an instance of a type. This can either be an explicitly registered type (service locator), or a type to perform constructor injection on using registered types.
		/// </summary>
		T Resolve<T>() where T : class;

		/// <summary>
		/// Registers a dependency that is constructed again each time it is requested using a factory delegate method. Will overwrite any existing dependency for this type.
		/// </summary>
		/// <typeparam name="TType">The dependency type being registered (interface or abstract class)</typeparam>
		/// <param name="instanceFactory">Method invoked to construct the dependency when requested</param>
		void Register<TType>(Func<TType> instanceFactory)
			where TType : class;
	}
}
