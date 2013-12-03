using System;

namespace Unicorn.Dependencies
{
	public interface IDependencyRegistry
	{
		T Resolve<T>() where T : class;

		void RegisterSingleton<TType, TInstance>()
			where TType : class
			where TInstance : class, TType;

		void RegisterPerRequestSingleton<TType, TInstance>()
			where TType : class
			where TInstance : class, TType;

		void RegisterInstanceFactory<TType>(Func<TType> instanceFactory)
			where TType : class;
	}
}
