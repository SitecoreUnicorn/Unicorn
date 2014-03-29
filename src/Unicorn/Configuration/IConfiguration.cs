using System;
using System.Collections.Generic;

namespace Unicorn.Configuration
{
	/// <summary>
	/// Represents a Unicorn configuration. A configuration is basically an instance of a DI container.
	/// </summary>
	public interface IConfiguration
	{
		/// <summary>
		/// The name of this configuration, used for display purposes
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Resolves an instance of a type. This can either be an explicitly registered type (service locator), or a type to perform constructor injection on using registered types.
		/// </summary>
		T Resolve<T>() where T : class;

		/// <summary>
		/// Registers a singleton instance of a dependency that is constructed only once per registry instance.
		/// </summary>
		/// <param name="type">The type to register with the configuration (e.g. interface)</param>
		/// <param name="implementation">The implementation of the type to return when the type is requested (e.g. concrete type)</param>
		/// <param name="singleInstance">If true, one instance is created and held on to by the configuration (singleton). If false, a new instance of the dependency is created each time it is requested.</param>
		/// <param name="unmappedConstructorParameters">This allows you to mix in constructor parameters that are not mapped to dependencies (e.g. strings, bools, etc). These are used alongside dependencies for constructor injection.</param>
		void Register(Type type, Type implementation, bool singleInstance, KeyValuePair<string, object>[] unmappedConstructorParameters);
	}
}
