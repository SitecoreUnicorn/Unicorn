using Kamsar.WebConsole;
using NUnit.Framework;
using Unicorn.Data;
using Unicorn.Dependencies;
using Unicorn.Dependencies.TinyIoC;
using Unicorn.Evaluators;
using Unicorn.Loader;

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
			registry.Register<IProgressStatus>(() => new StringProgressStatus());

			Assert.IsInstanceOf<SerializedAsMasterEvaluator>(registry.Resolve<IEvaluator>());
		}

		[Test]
		public void Resolve_ResolvesStandardConsistencyChecker()
		{
			var registry = new DefaultDependencyRegistry();
			registry.Register<IProgressStatus>(() => new StringProgressStatus());

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
