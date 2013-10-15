using System;
using System.Linq;
using Kamsar.WebConsole;
using Moq;
using NUnit.Framework;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Serialization;

namespace Unicorn.Tests.Evaluators
{
	[TestFixture]
	public class SerializedAsMasterEvaluatorTests
	{
		[Test]
		public void EvaluateOrphans_ThrowsArgumentNullException_WhenItemsAreNull()
		{
			var evaluator = CreateTestEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateOrphans(null));
		}

		[Test]
		public void EvaluateOrphans_RecyclesSingleOrphanItem()
		{
			var item = new Mock<ISourceItem>();
			item.Setup(x => x.Recycle());

			var evaluator = CreateTestEvaluator();

			evaluator.EvaluateOrphans(new[] { item.Object });

			item.Verify(x => x.Recycle(), Times.Exactly(1));
		}

		[Test]
		public void EvaluateOrphans_RecyclesMultipleOrphanItems()
		{
			var items = Enumerable.Range(1, 3).Select(x =>
				{
					var item = new Mock<ISourceItem>();
					item.Setup(y => y.Recycle());

					return item;
				}).ToArray();

			var evaluator = CreateTestEvaluator();

			evaluator.EvaluateOrphans(items.Select(x => x.Object).ToArray());

			foreach (var item in items)
				item.Verify(x => x.Recycle(), Times.Exactly(1));
		}

		[Test]
		public void EvaluateUpdate_ThrowsArgumentNullException_WhenSerializedItemIsNull()
		{
			var evaluator = CreateTestEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateUpdate(null, new Mock<ISourceItem>().Object));
		}

		[Test]
		public void EvaluateUpdate_ReturnsTrue_WhenExistingItemIsNull()
		{
			var evaluator = CreateTestEvaluator();

			Assert.IsTrue(evaluator.EvaluateUpdate(new Mock<ISerializedItem>().Object, null));
		}

		[Test]
		public void EvaluateUpdate_ReturnsTrue_WhenItemUpdatedDateIsNewer()
		{
			Assert.IsTrue(EvaluateUpdate_DateComparisonTest(new DateTime(2013, 1, 1), new DateTime(2012, 1, 1)));
		}

		[Test]
		public void EvaluateUpdate_ReturnsTrue_WhenItemUpdatedDateIsOlder()
		{
			Assert.IsTrue(EvaluateUpdate_DateComparisonTest(new DateTime(2012, 1, 1), new DateTime(2013, 1, 1)));
		}

		[Test]
		public void EvaluateUpdate_ReturnsTrue_WhenRevisionsAreUnequal()
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();
			item.Setup(x => x.GetLastModifiedDate("en", 1)).Returns(new DateTime(2013, 1, 1));
			item.Setup(x => x.GetRevision("en", 1)).Returns("SOURCE");
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			var version = SerializedVersionUtility.CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "SERIALIZED");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			Assert.IsTrue(evaluator.EvaluateUpdate(serialized.Object, item.Object));
		}

		[Test]
		public void EvaluateUpdate_ReturnsTrue_WhenNamesAreUnequal()
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();
			item.Setup(x => x.GetLastModifiedDate("en", 1)).Returns(new DateTime(2013, 1, 1));
			item.Setup(x => x.GetRevision("en", 1)).Returns("REVISION");
			item.Setup(x => x.Name).Returns("SOURCE");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("SERIALIZED");
			var version = SerializedVersionUtility.CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			Assert.IsTrue(evaluator.EvaluateUpdate(serialized.Object, item.Object));
		}

		[Test]
		public void EvaluateUpdate_ReturnsFalse_WhenDateRevisionNameMatch()
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();
			item.Setup(x => x.GetLastModifiedDate("en", 1)).Returns(new DateTime(2013, 1, 1));
			item.Setup(x => x.GetRevision("en", 1)).Returns("REVISION");
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			var version = SerializedVersionUtility.CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			Assert.IsFalse(evaluator.EvaluateUpdate(serialized.Object, item.Object));
		}

		private bool EvaluateUpdate_DateComparisonTest(DateTime sourceModified, DateTime serializedModified)
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();
			item.Setup(x => x.GetLastModifiedDate("en", 1)).Returns(sourceModified);
			item.Setup(x => x.GetRevision("en", 1)).Returns("REVISION");
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			var version = SerializedVersionUtility.CreateTestVersion("en", 1, serializedModified, "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			return evaluator.EvaluateUpdate(serialized.Object, item.Object);
		}

		private SerializedAsMasterEvaluator CreateTestEvaluator()
		{
			var logger = new ConsoleSerializedAsMasterEvaluatorLogger(new StringProgressStatus());
			
			return new SerializedAsMasterEvaluator(logger);
		}
	}
}
