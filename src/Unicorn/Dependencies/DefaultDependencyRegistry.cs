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
	public class DefaultDependencyRegistry : IDependencyRegistry
	{
		readonly TinyIoCContainer _container = new TinyIoCContainer();

		public DefaultDependencyRegistry()
		{
			RegisterSingleton<ISourceDataProvider, SitecoreSourceDataProvider>();
			RegisterPerRequestSingleton<IEvaluator, SerializedAsMasterEvaluator>();
			RegisterPerRequestSingleton<ISerializedAsMasterEvaluatorLogger, ConsoleSerializedAsMasterEvaluatorLogger>();

			RegisterPerRequestSingleton<IConsistencyChecker, DuplicateIdConsistencyChecker>();
			RegisterPerRequestSingleton<IDuplicateIdConsistencyCheckerLogger, ConsoleDuplicateIdConsistencyCheckerLogger>();
			
			RegisterPerRequestSingleton<IDeserializeFailureRetryer, DeserializeFailureRetryer>();
			RegisterPerRequestSingleton<ISerializationLoaderLogger, ConsoleSerializationLoaderLogger>();

			RegisterInstanceFactory<IPredicate>(() => new SerializationPresetPredicate(Resolve<ISourceDataProvider>()));

			RegisterInstanceFactory<IFiatDeserializerLogger>(() =>
			{
				// this allows resolving Fiat's logger regardless of whether it's writing to a console or not
				if (_container.CanResolve<IProgressStatus>())
					return new ConsoleFiatDeserializerLogger(Resolve<IProgressStatus>());

				return new NullFiatDeserializerLogger();
			});
			RegisterInstanceFactory<ISerializationProvider>(() => new FiatSitecoreSerializationProvider(
				predicate: Resolve<IPredicate>(),
				logger: Resolve<IFiatDeserializerLogger>()));

			RegisterSingleton<IUnicornDataProviderLogger, SitecoreLogUnicornDataProviderLogger>();
		}

		public T Resolve<T>()
			where T : class
		{
			return _container.Resolve<T>();
		}

		public void RegisterSingleton<TType, TInstance>()
			where TType : class
			where TInstance : class, TType
		{
			_container.Register<TType, TInstance>().AsSingleton();
		}

		public void RegisterInstanceFactory<TType>(Func<TType> instanceFactory) where TType : class
		{
// ReSharper disable once RedundantTypeArgumentsOfMethod
			_container.Register<TType>((container, overloads) => instanceFactory());
		}

		public void RegisterPerRequestSingleton<TType, TInstance>()
			where TType : class
			where TInstance : class, TType
		{
			_container.Register<TType, TInstance>().AsPerRequestSingleton();
		}
	}
}
