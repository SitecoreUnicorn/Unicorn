using System;
using Kamsar.WebConsole;
using NUnit.Framework;
using Unicorn.Data;
using Unicorn.Dependencies;
using Unicorn.Dependencies.TinyIoC;
using Unicorn.Evaluators;
using Unicorn.Loader;
using Unicorn.Predicates;
using Unicorn.Serialization;
using Unicorn.Serialization.Sitecore;

namespace Unicorn.Tests.Dependencies
{
	[TestFixture]
	public class DefaultDependencyRegistryTests
	{
		[Test]
		public void Resolve_ResolvesStandardSourceItemProvider()
		{
			var registry = new DefaultDependencyRegistry();

			Assert.IsInstanceOf<SitecoreSourceDataProvider>(registry.Resolve<ISourceDataProvider>());
		}

		[Test]
		public void Resolve_CannotResolveStandardEvaluatorWithoutWebConsole()
		{
			var registry = new DefaultDependencyRegistry();

			Assert.Throws<TinyIoCResolutionException>((() => registry.Resolve<IEvaluator>()));
		}

		[Test]
		public void Resolve_ResolvesStandardEvaluatorWithWebConsole()
		{
			var registry = new DefaultDependencyRegistry();
			registry.RegisterSingleton<IProgressStatus, StringProgressStatus>();

			Assert.IsInstanceOf<SerializedAsMasterEvaluator>(registry.Resolve<IEvaluator>());
		}

		[Test]
		public void Resolve_ResolvesStandardConsistencyChecker()
		{
			var registry = new DefaultDependencyRegistry();
			registry.RegisterSingleton<IProgressStatus, StringProgressStatus>();

			Assert.IsInstanceOf<DuplicateIdConsistencyChecker>(registry.Resolve<IConsistencyChecker>());
		}

		[Test]
		public void Resolve_ResolvesStandardFailureRetryer()
		{
			var registry = new DefaultDependencyRegistry();

			Assert.IsInstanceOf<DeserializeFailureRetryer>(registry.Resolve<IDeserializeFailureRetryer>());
		}
	}
}
