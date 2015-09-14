using System.Collections.Generic;
using System;
using NSubstitute;
using Xunit;
using Rainbow.Model;
using Rainbow.Predicates;
using Rainbow.Tests;
using Sitecore.Data;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Loader;
using Unicorn.Logging;
using Unicorn.Predicates;

namespace Unicorn.Tests.Loader
{
	public class LoaderTests
	{
		[Fact]
		public void LoadTree_ThrowsError_WhenRootItemIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => CreateTestLoader().LoadTree(null, Substitute.For<IDeserializeFailureRetryer>(), Substitute.For<IConsistencyChecker>()));
		}

		[Fact]
		public void LoadTree_ThrowsError_WhenRetryerIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => CreateTestLoader().LoadTree(Substitute.For<IItemData>(), null, Substitute.For<IConsistencyChecker>()));
		}

		[Fact]
		public void LoadTree_ThrowsError_WhenConsistencyCheckerIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => CreateTestLoader().LoadTree(Substitute.For<IItemData>(), Substitute.For<IDeserializeFailureRetryer>(), null));
		}

		[Fact]
		public void LoadTree_SkipsRootWhenExcluded()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = new FakeItem();

			var predicate = CreateExclusiveTestPredicate();

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(It.IsAny<ISourceItem>())).Returns(serializedRootItem);

			var logger = Substitute.For<ISerializationLoaderLogger>();

			var loader = CreateTestLoader(null, serializationProvider, predicate, null, logger);

			TestLoadTree(loader, serializedRootItem);

			logger.Verify(x => x.SkippedItemPresentInSerializationProvider(serializedRootItem, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
		}

		[Fact]
		public void LoadTree_LoadsRootWhenIncluded()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = new FakeItem("Test");

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(It.IsAny<ISourceItem>())).Returns(serializedRootItem);

			serializedRootItem.Setup(x => x.Deserialize(false)).Returns(root);

			var evaluator = Substitute.For<IEvaluator>();

			var loader = CreateTestLoader(null, serializationProvider, predicate, evaluator, null);

			TestLoadTree(loader, serializedRootItem);

			evaluator.Verify(x => x.EvaluateNewSerializedItem(serializedRootItem));
		}

		[Fact]
		public void LoadTree_SkipsChildOfRootWhenExcluded()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = new FakeItem("Root");

			var predicate = CreateExclusiveTestPredicate(new[] { serializedRootItem }, new[] { root });

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

			var sourceDataProvider = Substitute.For<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById(It.IsAny<string>(), It.IsAny<ID>())).Returns(root);

			var logger = Substitute.For<ISerializationLoaderLogger>();

			var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, null, logger);

			TestLoadTree(loader, serializedRootItem);

			logger.Verify(x => x.SkippedItem(root.Children[0], It.IsAny<string>(), It.IsAny<string>()));
		}

		[Fact]
		public void LoadTree_LoadsChildOfRootWhenIncluded()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = new FakeItem("Root");
			serializedRootItem.SetupGet(y => y.DatabaseName).Returns("root");


			var serializedChildItem = new FakeItem("Child");
			serializedChildItem.SetupGet(y => y.DatabaseName).Returns("child");
			serializedChildItem.Setup(x => x.Deserialize(false)).Returns(root.Children[0]);

			serializedRootItem.Setup(x => x.GetChildItems()).Returns(new[] { serializedChildItem });

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetReference(root.Children[0])).Returns(serializedChildItem);

			var sourceDataProvider = Substitute.For<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById("root", It.IsAny<ID>())).Returns(root);
			sourceDataProvider.Setup(x => x.GetItemById("child", It.IsAny<ID>())).Returns(root.Children[0]);

			var logger = Substitute.For<ISerializationLoaderLogger>();

			var evaluator = Substitute.For<IEvaluator>();
			evaluator.Setup(x => x.EvaluateUpdate(serializedChildItem, root.Children[0])).Returns(root.Children[0]);

			var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, logger);

			TestLoadTree(loader, serializedRootItem);

			evaluator.Verify(x => x.EvaluateUpdate(serializedChildItem, root.Children[0]));
		}

		[Fact]
		public void LoadTree_WarnsIfSkippedItemExistsInSerializationProvider()
		{

		}

		[Fact]
		public void LoadTree_IdentifiesOrphanChildItem()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = new FakeItem("Root");
			serializedRootItem.SetupGet(y => y.DatabaseName).Returns("flag");

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

			var sourceDataProvider = Substitute.For<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);

			var evaluator = Substitute.For<IEvaluator>();

			var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, null);

			TestLoadTree(loader, serializedRootItem);

			evaluator.Verify(x => x.EvaluateOrphans(It.Is<ISourceItem[]>(y => y.Contains(root.Children[0]))));
		}

		[Fact]
		public void LoadTree_DoesNotIdentifyValidChildrenAsOrphans()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = new FakeItem("Root");
			serializedRootItem.SetupGet(y => y.DatabaseName).Returns("flag");

			var serializedChildItem = new FakeItem("Child");
			serializedChildItem.SetupGet(y => y.DatabaseName).Returns("childflag");

			serializedRootItem.Setup(x => x.GetChildItems()).Returns(new[] { serializedChildItem });

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

			var sourceDataProvider = Substitute.For<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);
			sourceDataProvider.Setup(x => x.GetItemById("childflag", It.IsAny<ID>())).Returns(root.Children[0]);

			var evaluator = Substitute.For<IEvaluator>();

			var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, null);

			TestLoadTree(loader, serializedRootItem);

			evaluator.Verify(x => x.EvaluateOrphans(It.IsAny<ISourceItem[]>()), Times.Never());
		}

		[Fact]
		public void LoadTree_DoesNotIdentifySkippedItemsAsOrphans()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = new FakeItem("Root");

			var predicate = CreateExclusiveTestPredicate(new[] { serializedRootItem }, new[] { root });

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

			var sourceDataProvider = Substitute.For<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById(It.IsAny<string>(), It.IsAny<ID>())).Returns(root);

			var evaluator = Substitute.For<IEvaluator>();

			var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, null);

			TestLoadTree(loader, serializedRootItem);

			evaluator.Verify(x => x.EvaluateOrphans(It.IsAny<ISourceItem[]>()), Times.Never());
		}

		[Fact]
		public void LoadTree_UpdatesWhenItemDoesNotExistInSource()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = new FakeItem("Root");
			serializedRootItem.SetupGet(y => y.DatabaseName).Returns("flag");

			var serializedChildItem = new FakeItem("Child");

			serializedRootItem.Setup(x => x.GetChildItems()).Returns(new[] { serializedChildItem });

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

			var sourceDataProvider = Substitute.For<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);

			var evaluator = Substitute.For<IEvaluator>();

			var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, null);

			TestLoadTree(loader, serializedRootItem);

			evaluator.Verify(x => x.EvaluateNewSerializedItem(serializedChildItem));
		}

		[Fact]
		public void LoadTree_AbortsWhenConsistencyCheckFails()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = new FakeItem("Test");
			serializedRootItem.Setup(x => x.Deserialize(false)).Returns(root);

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = Substitute.For<ITargetDataStore>();
			serializationProvider.Setup(x => x.GetReference(It.IsAny<ISourceItem>())).Returns(serializedRootItem);

			var logger = Substitute.For<ISerializationLoaderLogger>();
			var consistencyChecker = Substitute.For<IConsistencyChecker>();

			consistencyChecker.Setup(x => x.IsConsistent(It.IsAny<IItemData>())).Returns(false);

			var loader = CreateTestLoader(null, serializationProvider, predicate, null, logger);

			Assert.Throws<ConsistencyException>((() => loader.LoadTree(serializedRootItem, Substitute.For<IDeserializeFailureRetryer>(), consistencyChecker)));
		}

		private SerializationLoader CreateTestLoader(ISourceDataStore sourceDataStore = null, ITargetDataStore targetDataStore = null, IPredicate predicate = null, IEvaluator evaluator = null, ISerializationLoaderLogger logger = null)
		{
			if (targetDataStore == null) targetDataStore = Substitute.For<ITargetDataStore>();
			if (sourceDataStore == null) sourceDataStore = Substitute.For<ISourceDataStore>();
			if (predicate == null) predicate = Substitute.For<IPredicate>();
			if (evaluator == null) evaluator = Substitute.For<IEvaluator>();
			if (logger == null) logger = Substitute.For<ISerializationLoaderLogger>();
			var mockLogger2 = Substitute.For<ILogger>();

			var pathResolver = new PredicateRootPathResolver(predicate, targetDataStore, sourceDataStore, mockLogger2);

			return new SerializationLoader(targetDataStore, sourceDataStore, predicate, evaluator, logger, pathResolver);
		}

		private ISourceItem CreateTestTree(int depth)
		{
			if (depth == 0) return null;

			var source = Substitute.For<ISourceItem>();
			source.SetupGet(x => x.Name).Returns("Item " + depth);
			source.SetupGet(x => x.Id).Returns(ID.NewID);

			if (depth > 1)
				source.SetupGet(x => x.Children).Returns(new[] { CreateTestTree(depth - 1) });

			return source;
		}

		private IPredicate CreateExclusiveTestPredicate(IEnumerable<ISerializedReference> includeReferences = null, IEnumerable<ISourceItem> includeItems = null)
		{
			var predicate = Substitute.For<IPredicate>();
			predicate.Setup(x => x.Includes(It.IsAny<ISourceItem>())).Returns(() => new PredicateResult(false));
			predicate.Setup(x => x.Includes(It.IsAny<ISerializedReference>())).Returns(() => new PredicateResult(false));

			if (includeReferences != null)
			{
				foreach (var include in includeReferences)
				{
					var includeItem = include;
					predicate.Setup(x => x.Includes(includeItem)).Returns(new PredicateResult(true));
				}
			}

			if (includeItems != null)
			{
				foreach (var include in includeItems)
				{
					var includeItem = include;
					predicate.Setup(x => x.Includes(includeItem)).Returns(new PredicateResult(true));
				}
			}

			return predicate;
		}

		private IPredicate CreateInclusiveTestPredicate()
		{
			var predicate = Substitute.For<IPredicate>();
			predicate.Includes(Arg.Any<IItemData>()).Returns(new PredicateResult(true));

			return predicate;
		}

		private void TestLoadTree(SerializationLoader loader, IItemData root, IDeserializeFailureRetryer retryer = null, IConsistencyChecker consistencyChecker = null)
		{
			if (retryer == null) retryer = Substitute.For<IDeserializeFailureRetryer>();
			if (consistencyChecker == null)
			{
				var checker = Substitute.For<IConsistencyChecker>();
				checker.IsConsistent(Arg.Any<IItemData>()).Returns(true);
				consistencyChecker = checker;
			}

			loader.LoadTree(root, retryer, consistencyChecker);
		}
	}
}
