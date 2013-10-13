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
			var evaluator = new SerializedAsMasterEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateOrphans(null, new StringProgressStatus()));
		}

		[Test]
		public void EvaluateOrphans_ThrowsArgumentNullException_WhenProgressIsNull()
		{
			var evaluator = new SerializedAsMasterEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateOrphans(new[] { new Mock<ISourceItem>().Object }, null));
		}

		[Test]
		public void EvaluateOrphans_RecyclesSingleOrphanItem()
		{
			var item = new Mock<ISourceItem>();
			item.Setup(x => x.Recycle());

			var evaluator = new SerializedAsMasterEvaluator();

			evaluator.EvaluateOrphans(new[] { item.Object }, new StringProgressStatus());

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

			var evaluator = new SerializedAsMasterEvaluator();

			evaluator.EvaluateOrphans(items.Select(x => x.Object).ToArray(), new StringProgressStatus());

			foreach (var item in items)
				item.Verify(x => x.Recycle(), Times.Exactly(1));
		}

		[Test]
		public void EvaluateUpdate_ThrowsArgumentNullException_WhenSerializedItemIsNull()
		{
			var evaluator = new SerializedAsMasterEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateUpdate(null, new Mock<ISourceItem>().Object, new StringProgressStatus()));
		}

		[Test]
		public void EvaluateUpdate_ThrowsArgumentNullException_WhenProgressIsNull()
		{
			var evaluator = new SerializedAsMasterEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateUpdate(new Mock<ISerializedItem>().Object, new Mock<ISourceItem>().Object, null));
		}

		[Test]
		public void EvaluateUpdate_ReturnsTrue_WhenExistingItemIsNull()
		{
			var evaluator = new SerializedAsMasterEvaluator();

			Assert.IsTrue(evaluator.EvaluateUpdate(new Mock<ISerializedItem>().Object, null, new StringProgressStatus()));
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
			var evaluator = new SerializedAsMasterEvaluator();

			var item = new Mock<ISourceItem>();
			item.Setup(x => x.GetLastModifiedDate("en", 1)).Returns(new DateTime(2013, 1, 1));
			item.Setup(x => x.GetRevision("en", 1)).Returns("SOURCE");
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			var version = SerializedVersionUtility.CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "SERIALIZED");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			Assert.IsTrue(evaluator.EvaluateUpdate(serialized.Object, item.Object, new StringProgressStatus()));
		}

		[Test]
		public void EvaluateUpdate_ReturnsTrue_WhenNamesAreUnequal()
		{
			var evaluator = new SerializedAsMasterEvaluator();

			var item = new Mock<ISourceItem>();
			item.Setup(x => x.GetLastModifiedDate("en", 1)).Returns(new DateTime(2013, 1, 1));
			item.Setup(x => x.GetRevision("en", 1)).Returns("REVISION");
			item.Setup(x => x.Name).Returns("SOURCE");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("SERIALIZED");
			var version = SerializedVersionUtility.CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			Assert.IsTrue(evaluator.EvaluateUpdate(serialized.Object, item.Object, new StringProgressStatus()));
		}

		[Test]
		public void EvaluateUpdate_ReturnsFalse_WhenDateRevisionNameMatch()
		{
			var evaluator = new SerializedAsMasterEvaluator();

			var item = new Mock<ISourceItem>();
			item.Setup(x => x.GetLastModifiedDate("en", 1)).Returns(new DateTime(2013, 1, 1));
			item.Setup(x => x.GetRevision("en", 1)).Returns("REVISION");
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			var version = SerializedVersionUtility.CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			Assert.IsFalse(evaluator.EvaluateUpdate(serialized.Object, item.Object, new StringProgressStatus()));
		}

		private bool EvaluateUpdate_DateComparisonTest(DateTime sourceModified, DateTime serializedModified)
		{
			var evaluator = new SerializedAsMasterEvaluator();

			var item = new Mock<ISourceItem>();
			item.Setup(x => x.GetLastModifiedDate("en", 1)).Returns(sourceModified);
			item.Setup(x => x.GetRevision("en", 1)).Returns("REVISION");
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			var version = SerializedVersionUtility.CreateTestVersion("en", 1, serializedModified, "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			return evaluator.EvaluateUpdate(serialized.Object, item.Object, new StringProgressStatus());
		}
	}
}
