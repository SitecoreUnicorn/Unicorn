using System;
using Kamsar.WebConsole;
using Unicorn.Data;
using Unicorn.Dependencies.TinyIoC;
using Unicorn.Evaluators;
using Unicorn.Loader;
using Unicorn.Predicates;
using Unicorn.Serialization;
using Unicorn.Serialization.Sitecore.Fiat;

namespace Unicorn.Dependencies
{
	/// <summary>
	/// Defines the default dependency configuration for a production Unicorn instance.
	/// </summary>
	public class DefaultDependencyRegistry : IDependencyRegistry
	{
		protected readonly TinyIoCContainer Container = new TinyIoCContainer();

		public DefaultDependencyRegistry()
		{
// ReSharper disable DoNotCallOverridableMethodsInConstructor
			RegisterSourceDataProvider();
			RegisterEvaluator();
			RegisterLoader();
			RegisterPredicate();
			RegisterSerializationProvider();
			RegisterDataProvider();
// ReSharper restore DoNotCallOverridableMethodsInConstructor
		}

		/// <summary>
		/// Resolves a requested type, either directly registered or via constructor injection
		/// </summary>
		public T Resolve<T>()
			where T : class
		{
			return Container.Resolve<T>();
		}

		/// <summary>
		/// Registers a dependency that is constructed again each time it is requested using a factory delegate method. Will overwrite any existing dependency for this type.
		/// </summary>
		/// <typeparam name="TType">The dependency type being registered (interface or abstract class)</typeparam>
		/// <param name="instanceFactory">Method invoked to construct the dependency when requested</param>
		public void Register<TType>(Func<TType> instanceFactory) where TType : class
		{
			Container.Register((container, overloads) => instanceFactory());
		}

		/// <summary>
		/// Registers a dependency with TinyIoC that is constructed once per HTTP request
		/// </summary>
		protected virtual void RegisterPerRequestSingleton<TType, TInstance>()
			where TType : class
			where TInstance : class, TType
		{
			Container.Register<TType, TInstance>().AsPerRequestSingleton();
		}

		/// <summary>
		/// Override this method to inject your own ISourceDataProvider implementation
		/// </summary>
		protected virtual void RegisterSourceDataProvider()
		{
			RegisterPerRequestSingleton<ISourceDataProvider, SitecoreSourceDataProvider>();
		}

		/// <summary>
		/// Override this method to inject your own IEvaluator implementation
		/// </summary>
		protected virtual void RegisterEvaluator()
		{
			RegisterPerRequestSingleton<IEvaluator, SerializedAsMasterEvaluator>();
			RegisterPerRequestSingleton<ISerializedAsMasterEvaluatorLogger, ConsoleSerializedAsMasterEvaluatorLogger>();
		}

		/// <summary>
		/// Override this method to inject custom dependencies for SerializationLoader
		/// </summary>
		protected virtual void RegisterLoader()
		{
			RegisterPerRequestSingleton<IConsistencyChecker, DuplicateIdConsistencyChecker>();
			RegisterPerRequestSingleton<IDuplicateIdConsistencyCheckerLogger, ConsoleDuplicateIdConsistencyCheckerLogger>();
			RegisterPerRequestSingleton<IDeserializeFailureRetryer, DeserializeFailureRetryer>();
			RegisterPerRequestSingleton<ISerializationLoaderLogger, ConsoleSerializationLoaderLogger>();
		}

		/// <summary>
		/// Override this method to inject your own IPredicate implementation
		/// </summary>
		protected virtual void RegisterPredicate()
		{
			Register<IPredicate>(() => new SerializationPresetPredicate(Resolve<ISourceDataProvider>(), "default"));
		}

		/// <summary>
		/// Override this method to inject your own ISerializationProvider implementation
		/// </summary>
		protected virtual void RegisterSerializationProvider()
		{
			Register<IFiatDeserializerLogger>(() =>
			{
				// this allows resolving Fiat's logger regardless of whether it's writing to a console or not
				if (Container.CanResolve<IProgressStatus>())
					return Resolve<ConsoleFiatDeserializerLogger>();

				return new NullFiatDeserializerLogger();
			});

			// note making use of default param values to FiatSitecoreSerializationProvider here
			Register<ISerializationProvider>(() => new FiatSitecoreSerializationProvider(Resolve<IPredicate>(), Resolve<IFiatDeserializerLogger>()));
		}

		/// <summary>
		/// Override this method to inject your own data provider logger implementation
		/// </summary>
		protected virtual void RegisterDataProvider()
		{
			RegisterPerRequestSingleton<IUnicornDataProviderLogger, SitecoreLogUnicornDataProviderLogger>();
		}
	}
}
