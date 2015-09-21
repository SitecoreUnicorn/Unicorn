using System.Collections.Generic;
using System;
using System.Linq;
using NSubstitute;
using NSubstitute.Core;
using Xunit;
using Rainbow.Model;
using Rainbow.Tests;
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
			var serializedRootItem = new FakeItem();
			var predicate = CreateExclusiveTestPredicate();
			var logger = Substitute.For<ISerializationLoaderLogger>();
			var loader = CreateTestLoader(predicate: predicate, logger: logger);

			TestLoadTree(loader, serializedRootItem);

			logger.Received().SkippedItemPresentInSerializationProvider(serializedRootItem, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
		}

		[Fact]
		public void LoadTree_LoadsRootWhenIncluded()
		{
			var serializedRootItem = new FakeItem();
			var evaluator = Substitute.For<IEvaluator>();
			var sourceData = Substitute.For<ISourceDataStore>();
			sourceData.GetByPathAndId(serializedRootItem.Path, serializedRootItem.Id, serializedRootItem.DatabaseName).Returns((IItemData)null);

			var loader = CreateTestLoader(evaluator: evaluator, sourceDataStore: sourceData);

			TestLoadTree(loader, serializedRootItem);

			evaluator.Received().EvaluateNewSerializedItem(serializedRootItem);
		}

		[Fact]
		public void LoadTree_SkipsChildOfRootWhenExcluded()
		{
			var dataStore = Substitute.For<ITargetDataStore>();
			var root = new FakeItem();
			var child = new FakeItem(parentId:root.Id, id:Guid.NewGuid());
			dataStore.GetChildren(root).Returns(new[] {child});

			var predicate = CreateExclusiveTestPredicate(new[] { root });
			var logger = Substitute.For<ISerializationLoaderLogger>();
			var loader = CreateTestLoader(predicate: predicate, logger:logger, targetDataStore: dataStore);

			TestLoadTree(loader, root);

			logger.Received().SkippedItemPresentInSerializationProvider(Arg.Is<IItemData>(data => data.Id == child.Id), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
		}

		[Fact]
		public void LoadTree_LoadsChildOfRootWhenIncluded()
		{
			var dataStore = Substitute.For<ITargetDataStore>();
			var root = new FakeItem();
			var child = new FakeItem(parentId: root.Id, id: Guid.NewGuid());
			dataStore.GetChildren(root).Returns(new[] { child });
			
			var evaluator = Substitute.For<IEvaluator>();
			var loader = CreateTestLoader(evaluator: evaluator, targetDataStore: dataStore);

			TestLoadTree(loader, root);

			evaluator.Received().EvaluateUpdate(Arg.Any<IItemData>(), child);
		}

		//[Fact]
		//public void LoadTree_IdentifiesOrphanChildItem()
		//{
		//	var root = CreateTestTree(2);

		//	var serializedRootItem = new FakeItem("Root");
		//	serializedRootItem.SetupGet(y => y.DatabaseName).Returns("flag");

		//	var predicate = CreateInclusiveTestPredicate();

		//	var serializationProvider = Substitute.For<ITargetDataStore>();
		//	serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

		//	var sourceDataProvider = Substitute.For<ISourceDataProvider>();
		//	sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);

		//	var evaluator = Substitute.For<IEvaluator>();

		//	var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, null);

		//	TestLoadTree(loader, serializedRootItem);

		//	evaluator.Verify(x => x.EvaluateOrphans(It.Is<ISourceItem[]>(y => y.Contains(root.Children[0]))));
		//}

		//[Fact]
		//public void LoadTree_DoesNotIdentifyValidChildrenAsOrphans()
		//{
		//	var root = CreateTestTree(2);

		//	var serializedRootItem = new FakeItem("Root");
		//	serializedRootItem.SetupGet(y => y.DatabaseName).Returns("flag");

		//	var serializedChildItem = new FakeItem("Child");
		//	serializedChildItem.SetupGet(y => y.DatabaseName).Returns("childflag");

		//	serializedRootItem.Setup(x => x.GetChildItems()).Returns(new[] { serializedChildItem });

		//	var predicate = CreateInclusiveTestPredicate();

		//	var serializationProvider = Substitute.For<ITargetDataStore>();
		//	serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

		//	var sourceDataProvider = Substitute.For<ISourceDataProvider>();
		//	sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);
		//	sourceDataProvider.Setup(x => x.GetItemById("childflag", It.IsAny<ID>())).Returns(root.Children[0]);

		//	var evaluator = Substitute.For<IEvaluator>();

		//	var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, null);

		//	TestLoadTree(loader, serializedRootItem);

		//	evaluator.Verify(x => x.EvaluateOrphans(It.IsAny<ISourceItem[]>()), Times.Never());
		//}

		//[Fact]
		//public void LoadTree_DoesNotIdentifySkippedItemsAsOrphans()
		//{
		//	var root = CreateTestTree(2);

		//	var serializedRootItem = new FakeItem("Root");

		//	var predicate = CreateExclusiveTestPredicate(new[] { serializedRootItem }, new[] { root });

		//	var serializationProvider = Substitute.For<ITargetDataStore>();
		//	serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

		//	var sourceDataProvider = Substitute.For<ISourceDataProvider>();
		//	sourceDataProvider.Setup(x => x.GetItemById(It.IsAny<string>(), It.IsAny<ID>())).Returns(root);

		//	var evaluator = Substitute.For<IEvaluator>();

		//	var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, null);

		//	TestLoadTree(loader, serializedRootItem);

		//	evaluator.Verify(x => x.EvaluateOrphans(It.IsAny<ISourceItem[]>()), Times.Never());
		//}

		//[Fact]
		//public void LoadTree_UpdatesWhenItemDoesNotExistInSource()
		//{
		//	var root = CreateTestTree(1);

		//	var serializedRootItem = new FakeItem("Root");
		//	serializedRootItem.SetupGet(y => y.DatabaseName).Returns("flag");

		//	var serializedChildItem = new FakeItem("Child");

		//	serializedRootItem.Setup(x => x.GetChildItems()).Returns(new[] { serializedChildItem });

		//	var predicate = CreateInclusiveTestPredicate();

		//	var serializationProvider = Substitute.For<ITargetDataStore>();
		//	serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);

		//	var sourceDataProvider = Substitute.For<ISourceDataProvider>();
		//	sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);

		//	var evaluator = Substitute.For<IEvaluator>();

		//	var loader = CreateTestLoader(sourceDataProvider, serializationProvider, predicate, evaluator, null);

		//	TestLoadTree(loader, serializedRootItem);

		//	evaluator.Verify(x => x.EvaluateNewSerializedItem(serializedChildItem));
		//}

		//[Fact]
		//public void LoadTree_AbortsWhenConsistencyCheckFails()
		//{
		//	var root = CreateTestTree(1);

		//	var serializedRootItem = new FakeItem("Test");
		//	serializedRootItem.Setup(x => x.Deserialize(false)).Returns(root);

		//	var predicate = CreateInclusiveTestPredicate();

		//	var serializationProvider = Substitute.For<ITargetDataStore>();
		//	serializationProvider.Setup(x => x.GetReference(It.IsAny<ISourceItem>())).Returns(serializedRootItem);

		//	var logger = Substitute.For<ISerializationLoaderLogger>();
		//	var consistencyChecker = Substitute.For<IConsistencyChecker>();

		//	consistencyChecker.Setup(x => x.IsConsistent(It.IsAny<IItemData>())).Returns(false);

		//	var loader = CreateTestLoader(null, serializationProvider, predicate, null, logger);

		//	Assert.Throws<ConsistencyException>((() => loader.LoadTree(serializedRootItem, Substitute.For<IDeserializeFailureRetryer>(), consistencyChecker)));
		//}

		private SerializationLoader CreateTestLoader(ISourceDataStore sourceDataStore = null, ITargetDataStore targetDataStore = null, IPredicate predicate = null, IEvaluator evaluator = null, ISerializationLoaderLogger logger = null)
		{
			if (targetDataStore == null) targetDataStore = Substitute.For<ITargetDataStore>();
			if (sourceDataStore == null) sourceDataStore = Substitute.For<ISourceDataStore>();
			if (predicate == null) predicate = CreateInclusiveTestPredicate();
			if (evaluator == null) evaluator = Substitute.For<IEvaluator>();
			if (logger == null) logger = Substitute.For<ISerializationLoaderLogger>();
			var mockLogger2 = Substitute.For<ILogger>();

			var pathResolver = new PredicateRootPathResolver(predicate, targetDataStore, sourceDataStore, mockLogger2);

			return new SerializationLoader(sourceDataStore, targetDataStore, predicate, evaluator, logger, pathResolver);
		}

		//private IItemData CreateTestTree(int depth)
		//{
		//	if (depth == 0) return null;

		//	var source = Substitute.For<ISourceItem>();
		//	source.SetupGet(x => x.Name).Returns("Item " + depth);
		//	source.SetupGet(x => x.Id).Returns(ID.NewID);

		//	if (depth > 1)
		//		source.SetupGet(x => x.Children).Returns(new[] { CreateTestTree(depth - 1) });

		//	return source;
		//}

		private IPredicate CreateExclusiveTestPredicate(IEnumerable<IItemData> includes = null)
		{
			var predicate = Substitute.For<IPredicate>();
			predicate.Includes(Arg.Any<IItemData>()).Returns(delegate (CallInfo info)
			{
				if (includes != null && includes.Any(x=>x.Id == info.Arg<IItemData>().Id)) return new PredicateResult(true);
				return new PredicateResult(false);
			});

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
