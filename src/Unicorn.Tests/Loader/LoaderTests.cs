using System.Collections.Generic;
using System.Linq;
using System;
using Kamsar.WebConsole;
using Moq;
using NUnit.Framework;
using Sitecore.Data;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Loader;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.Tests.Loader
{
	[TestFixture]
	public class LoaderTests
	{
		[Test]
		public void ctor_ThrowsError_WhenSerializationProviderIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationLoader(null, new Mock<ISourceDataProvider>().Object, new Mock<IPredicate>().Object, new Mock<IEvaluator>().Object, new Mock<ISerializationLoaderLogger>().Object));
		}

		[Test]
		public void ctor_ThrowsError_WhenSourceDataProviderIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationLoader(new Mock<ISerializationProvider>().Object, null, new Mock<IPredicate>().Object, new Mock<IEvaluator>().Object, new Mock<ISerializationLoaderLogger>().Object));
		}

		[Test]
		public void ctor_ThrowsError_WhenPredicateIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationLoader(new Mock<ISerializationProvider>().Object, new Mock<ISourceDataProvider>().Object, null, new Mock<IEvaluator>().Object, new Mock<ISerializationLoaderLogger>().Object));
		}

		[Test]
		public void ctor_ThrowsError_WhenEvaluatorIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationLoader(new Mock<ISerializationProvider>().Object, new Mock<ISourceDataProvider>().Object, new Mock<IPredicate>().Object, null, new Mock<ISerializationLoaderLogger>().Object));
		}

		[Test]
		public void ctor_ThrowsError_WhenLoggerIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationLoader(new Mock<ISerializationProvider>().Object, new Mock<ISourceDataProvider>().Object, new Mock<IPredicate>().Object, new Mock<IEvaluator>().Object, null));
		}

		[Test]
		public void LoadTree_ThrowsError_WhenRootItemIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => CreateTestLoader(null, null, null, null, null).LoadTree(null, new Mock<IDeserializeFailureRetryer>().Object, new Mock<IConsistencyChecker>().Object));
		}

		[Test]
		public void LoadTree_ThrowsError_WhenRetryerIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => CreateTestLoader(null, null, null, null, null).LoadTree(new Mock<ISourceItem>().Object, null, new Mock<IConsistencyChecker>().Object));
		}

		[Test]
		public void LoadTree_ThrowsError_WhenConsistencyCheckerIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => CreateTestLoader(null, null, null, null, null).LoadTree(new Mock<ISourceItem>().Object, new Mock<IDeserializeFailureRetryer>().Object, null));
		}

		[Test]
		public void LoadTree_ThrowsError_WhenRootDoesNotExist()
		{
			var rootItem = CreateTestTree(1);

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(rootItem)).Returns((ISerializedReference)null);

			var progress = new StringProgressStatus();
			var logger = new ConsoleSerializationLoaderLogger(progress);

			var loader = new SerializationLoader(serializationProvider.Object, new Mock<ISourceDataProvider>().Object, new Mock<IPredicate>().Object, new Mock<IEvaluator>().Object, logger);

			Assert.Throws<InvalidOperationException>(() => TestLoadTree(loader, rootItem));
		}

		[Test]
		public void LoadTree_Retries_RetryableSingleItemFailure()
		{

		}

		[Test]
		public void LoadTree_Retries_RetryableReferenceFailure()
		{

		}

		[Test]
		public void LoadTree_Retries_StopsOnUnresolvableError()
		{

		}

		[Test]
		public void LoadTree_SkipsRootWhenExcluded()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = CreateSerializedItem("Test", null);

			var predicate = CreateExclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(It.IsAny<ISourceItem>())).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);

			var logger = new Mock<ISerializationLoaderLogger>();

			var loader = CreateTestLoader(serializationProvider.Object, null, predicate, null, logger.Object);

			TestLoadTree(loader, root);

			logger.Verify(x => x.SkippedItemPresentInSerializationProvider(serializedRootItem, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
		}

		[Test]
		public void LoadTree_LoadsRootWhenIncluded()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = CreateSerializedItem("Test", null);

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(It.IsAny<ISourceItem>())).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.DeserializeItem(serializedRootItem)).Returns(root);

			var logger = new Mock<ISerializationLoaderLogger>();

			var loader = CreateTestLoader(serializationProvider.Object, null, predicate, null, logger.Object);

			TestLoadTree(loader, root);

			serializationProvider.Verify(x => x.DeserializeItem(serializedRootItem));
			logger.Verify(x => x.SerializedNewItem(serializedRootItem));
		}

		[Test]
		public void LoadTree_SkipsChildOfRootWhenExcluded()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = CreateSerializedItem("Root", null);

			var predicate = CreateExclusiveTestPredicate(new[] { serializedRootItem }, new[] { root });

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);

			var sourceDataProvider = new Mock<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById(It.IsAny<string>(), It.IsAny<ID>())).Returns(root);

			var logger = new Mock<ISerializationLoaderLogger>();

			var loader = CreateTestLoader(serializationProvider.Object, sourceDataProvider.Object, predicate, null, logger.Object);

			TestLoadTree(loader, root);

			logger.Verify(x => x.SkippedItem(root.Children[0], It.IsAny<string>(), It.IsAny<string>()));
		}

		[Test]
		public void LoadTree_LoadsChildOfRootWhenIncluded()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = CreateSerializedItem("Root", x => x.SetupGet(y => y.DatabaseName).Returns("root"));
			var serializedChildItem = CreateSerializedItem("Child", x => x.SetupGet(y => y.DatabaseName).Returns("child"));

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetReference(root.Children[0])).Returns(serializedChildItem);
			serializationProvider.Setup(x => x.GetItem(serializedChildItem)).Returns(serializedChildItem);
			serializationProvider.Setup(x => x.GetChildItems(serializedRootItem)).Returns(new[] {serializedChildItem});
			serializationProvider.Setup(x => x.DeserializeItem(serializedChildItem)).Returns(root.Children[0]);

			var sourceDataProvider = new Mock<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById("root", It.IsAny<ID>())).Returns(root);
			sourceDataProvider.Setup(x => x.GetItemById("child", It.IsAny<ID>())).Returns(root.Children[0]);

			var logger = new Mock<ISerializationLoaderLogger>();

			var evaluator = new Mock<IEvaluator>();
			evaluator.Setup(x => x.EvaluateUpdate(serializedChildItem, root.Children[0])).Returns(true);

			var loader = CreateTestLoader(serializationProvider.Object, sourceDataProvider.Object, predicate, evaluator.Object, logger.Object);

			TestLoadTree(loader, root);

			serializationProvider.Verify(x => x.DeserializeItem(serializedChildItem));
			logger.Verify(x => x.SerializedUpdatedItem(serializedChildItem));
		}

		[Test]
		public void LoadTree_WarnsIfSkippedItemExistsInSerializationProvider()
		{

		}

		[Test]
		public void LoadTree_IdentifiesOrphanChildItem()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = CreateSerializedItem("Root", x => x.SetupGet(y => y.DatabaseName).Returns("flag"));

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);

			var sourceDataProvider = new Mock<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);

			var evaluator = new Mock<IEvaluator>();

			var loader = CreateTestLoader(serializationProvider.Object, sourceDataProvider.Object, predicate, evaluator.Object, null);

			TestLoadTree(loader, root);

			evaluator.Verify(x => x.EvaluateOrphans(It.Is<ISourceItem[]>(y => y.Contains(root.Children[0]))));
		}

		[Test]
		public void LoadTree_DoesNotIdentifyValidChildrenAsOrphans()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = CreateSerializedItem("Root", x => x.SetupGet(y => y.DatabaseName).Returns("flag"));
			var serializedChildItem = CreateSerializedItem("Child", x => x.SetupGet(y => y.DatabaseName).Returns("childflag"));

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetChildItems(serializedRootItem)).Returns(new[] {serializedChildItem});

			var sourceDataProvider = new Mock<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);
			sourceDataProvider.Setup(x => x.GetItemById("childflag", It.IsAny<ID>())).Returns(root.Children[0]);

			var evaluator = new Mock<IEvaluator>();

			var loader = CreateTestLoader(serializationProvider.Object, sourceDataProvider.Object, predicate, evaluator.Object, null);

			TestLoadTree(loader, root);

			evaluator.Verify(x => x.EvaluateOrphans(It.IsAny<ISourceItem[]>()), Times.Never());
		}

		[Test]
		public void LoadTree_DoesNotIdentifySkippedItemsAsOrphans()
		{
			var root = CreateTestTree(2);

			var serializedRootItem = CreateSerializedItem("Root", null);

			var predicate = CreateExclusiveTestPredicate(new[] { serializedRootItem }, new[] { root });

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);

			var sourceDataProvider = new Mock<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById(It.IsAny<string>(), It.IsAny<ID>())).Returns(root);

			var evaluator = new Mock<IEvaluator>();

			var loader = CreateTestLoader(serializationProvider.Object, sourceDataProvider.Object, predicate, evaluator.Object, null);

			TestLoadTree(loader, root);

			evaluator.Verify(x => x.EvaluateOrphans(It.IsAny<ISourceItem[]>()), Times.Never());
		}

		[Test]
		public void LoadTree_UpdatesItemWhenEvaluatorAllows()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = CreateSerializedItem("Root", null);

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.DeserializeItem(serializedRootItem)).Returns(root);

			var sourceDataProvider = new Mock<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById(It.IsAny<string>(), It.IsAny<ID>())).Returns(root);

			var evaluator = new Mock<IEvaluator>();
			evaluator.Setup(x => x.EvaluateUpdate(It.IsAny<ISerializedItem>(), It.IsAny<ISourceItem>())).Returns(true);

			var loader = CreateTestLoader(serializationProvider.Object, sourceDataProvider.Object, predicate, evaluator.Object, null);

			TestLoadTree(loader, root);

			evaluator.Verify(x => x.EvaluateUpdate(serializedRootItem, root));
			serializationProvider.Verify(x => x.DeserializeItem(serializedRootItem));
		}

		[Test]
		public void LoadTree_DoesNotUpdateItemWhenEvaluatorDenies()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = CreateSerializedItem("Root", null);

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);

			var sourceDataProvider = new Mock<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById(It.IsAny<string>(), It.IsAny<ID>())).Returns(root);

			var evaluator = new Mock<IEvaluator>();
			evaluator.Setup(x => x.EvaluateUpdate(It.IsAny<ISerializedItem>(), It.IsAny<ISourceItem>())).Returns(false);

			var loader = CreateTestLoader(serializationProvider.Object, sourceDataProvider.Object, predicate, evaluator.Object, null);

			TestLoadTree(loader, root);

			evaluator.Verify(x => x.EvaluateUpdate(serializedRootItem, root));
			serializationProvider.Verify(x => x.DeserializeItem(serializedRootItem), Times.Never());
		}

		[Test]
		public void LoadTree_UpdatesWhenItemDoesNotExistInSource()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = CreateSerializedItem("Root", x => x.SetupGet(y => y.DatabaseName).Returns("flag"));
			var serializedChildItem = CreateSerializedItem("Child", null);

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(root)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetChildItems(serializedRootItem)).Returns(new[] { serializedChildItem });

			var sourceDataProvider = new Mock<ISourceDataProvider>();
			sourceDataProvider.Setup(x => x.GetItemById("flag", It.IsAny<ID>())).Returns(root);

			var loader = CreateTestLoader(serializationProvider.Object, sourceDataProvider.Object, predicate, null, null);

			TestLoadTree(loader, root);

			serializationProvider.Verify(x => x.DeserializeItem(serializedChildItem));
		}

		[Test]
		public void LoadTree_AbortsWhenConsistencyCheckFails()
		{
			var root = CreateTestTree(1);

			var serializedRootItem = CreateSerializedItem("Test", null);

			var predicate = CreateInclusiveTestPredicate();

			var serializationProvider = new Mock<ISerializationProvider>();
			serializationProvider.Setup(x => x.GetReference(It.IsAny<ISourceItem>())).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.GetItem(serializedRootItem)).Returns(serializedRootItem);
			serializationProvider.Setup(x => x.DeserializeItem(serializedRootItem)).Returns(root);

			var logger = new Mock<ISerializationLoaderLogger>();
			var consistencyChecker = new Mock<IConsistencyChecker>();

			consistencyChecker.Setup(x => x.IsConsistent(It.IsAny<ISerializedItem>())).Returns(false);

			var loader = CreateTestLoader(serializationProvider.Object, null, predicate, null, logger.Object);

			Assert.Throws<ConsistencyException>((() => loader.LoadTree(root, new Mock<IDeserializeFailureRetryer>().Object, consistencyChecker.Object)));
		}

		private SerializationLoader CreateTestLoader(ISerializationProvider serializationProvider, ISourceDataProvider sourceDataProvider, IPredicate predicate, IEvaluator evaluator, ISerializationLoaderLogger logger)
		{
			var mockSerializationProvider = new Mock<ISerializationProvider>();
			var mockSourceDataProvider = new Mock<ISourceDataProvider>();
			var mockPredicate = new Mock<IPredicate>();
			var mockEvaluator = new Mock<IEvaluator>();
			var mockLogger = new Mock<ISerializationLoaderLogger>();

			return new SerializationLoader(serializationProvider ?? mockSerializationProvider.Object,
				sourceDataProvider ?? mockSourceDataProvider.Object,
				predicate ?? mockPredicate.Object,
				evaluator ?? mockEvaluator.Object,
				logger ?? mockLogger.Object);
		}

		private ISourceItem CreateTestTree(int depth)
		{
			if (depth == 0) return null;

			var source = new Mock<ISourceItem>();
			source.SetupGet(x => x.Name).Returns("Item " + depth);
			source.SetupGet(x => x.Id).Returns(ID.NewID);

			if(depth > 1)
				source.SetupGet(x => x.Children).Returns(new[] { CreateTestTree(depth - 1) });

			return source.Object;
		}

		private ISerializedItem CreateSerializedItem(string name, Action<Mock<ISerializedItem>> customize)
		{
			var serializedItem = new Mock<ISerializedItem>();
			serializedItem.SetupGet(x => x.Name).Returns(name);
			serializedItem.SetupGet(x => x.ItemPath).Returns("test");
			serializedItem.SetupGet(x => x.Id).Returns(ID.NewID);

			if (customize != null) customize(serializedItem);

			return serializedItem.Object;
		}

		private IPredicate CreateExclusiveTestPredicate(IEnumerable<ISerializedReference> includeReferences = null, IEnumerable<ISourceItem> includeItems = null)
		{
			var predicate = new Mock<IPredicate>();
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

			return predicate.Object;
		}

		private IPredicate CreateInclusiveTestPredicate()
		{
			var predicate = new Mock<IPredicate>();
			predicate.Setup(x => x.Includes(It.IsAny<ISourceItem>())).Returns(() => new PredicateResult(true));
			predicate.Setup(x => x.Includes(It.IsAny<ISerializedReference>())).Returns(() => new PredicateResult(true));

			return predicate.Object;
		}

		private void TestLoadTree(SerializationLoader loader, ISourceItem root, IDeserializeFailureRetryer retryer = null, IConsistencyChecker consistencyChecker = null)
		{
			if (retryer == null) retryer = new Mock<IDeserializeFailureRetryer>().Object;
			if (consistencyChecker == null)
			{
				var checker = new Mock<IConsistencyChecker>();
				checker.Setup(x => x.IsConsistent(It.IsAny<ISerializedItem>())).Returns(true);
				consistencyChecker = checker.Object;
			}

			loader.LoadTree(root, retryer, consistencyChecker);
		}
	}
}
