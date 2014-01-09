using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using Sitecore;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Serialization;

namespace Unicorn.Tests.Evaluator
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
		public void EvaluateUpdate_ThrowsArgumentNullException_WhenExistingItemIsNull()
		{
			var evaluator = CreateTestEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateUpdate(new Mock<ISerializedItem>().Object, null));
		}

		[Test]
		public void EvaluateUpdate_Deserializes_WhenItemUpdatedDateIsNewer()
		{
			Assert.IsTrue(EvaluateUpdate_DateComparisonTest(new DateTime(2013, 1, 1), new DateTime(2012, 1, 1)));
		}

		[Test]
		public void EvaluateUpdate_Deserializes_WhenItemUpdatedDateIsOlder()
		{
			Assert.IsTrue(EvaluateUpdate_DateComparisonTest(new DateTime(2012, 1, 1), new DateTime(2013, 1, 1)));
		}

		[Test]
		public void EvaluateUpdate_Deserializes_WhenRevisionsAreUnequal()
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();

			var sourceVersion = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "SOURCE");
			item.Setup(x => x.Versions).Returns(new[] { sourceVersion });
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			serialized.Setup(x => x.Deserialize(It.IsAny<bool>())).Returns(new Mock<ISourceItem>().Object);

			var serializedVersion = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "SERIALIZED");
			serialized.Setup(x => x.Versions).Returns(new[] { serializedVersion });

			evaluator.EvaluateUpdate(serialized.Object, item.Object);

			serialized.Verify(x => x.Deserialize(false));
		}

		[Test]
		public void EvaluateUpdate_Deserializes_WhenNewSerializedVersionExists()
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();

			var sourceVersion = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");
			item.Setup(x => x.Versions).Returns(new[] { sourceVersion });
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			serialized.Setup(x => x.Deserialize(It.IsAny<bool>())).Returns(new Mock<ISourceItem>().Object);

			var serializedVersion = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");
			var serializedVersion2 = CreateTestVersion("en", 2, new DateTime(2013, 1, 1), "REVISION");
			serialized.Setup(x => x.Versions).Returns(new[] { serializedVersion, serializedVersion2 });

			evaluator.EvaluateUpdate(serialized.Object, item.Object);

			serialized.Verify(x => x.Deserialize(false));
		}

		[Test]
		public void EvaluateUpdate_Deserializes_WhenNewSourceVersionExists()
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();

			var sourceVersion = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");
			var sourceVersion2 = CreateTestVersion("en", 2, new DateTime(2013, 1, 1), "REVISION");
			item.Setup(x => x.Versions).Returns(new[] { sourceVersion, sourceVersion2 });
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			serialized.Setup(x => x.Deserialize(It.IsAny<bool>())).Returns(new Mock<ISourceItem>().Object);

			var serializedVersion = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");
			serialized.Setup(x => x.Versions).Returns(new[] { serializedVersion });

			evaluator.EvaluateUpdate(serialized.Object, item.Object);

			serialized.Verify(x => x.Deserialize(false));
		}

		[Test]
		public void EvaluateUpdate_Deserializes_WhenNamesAreUnequal()
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();
			var sourceVersion = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");
			item.Setup(x => x.Versions).Returns(new[] { sourceVersion });
			item.Setup(x => x.Name).Returns("SOURCE");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("SERIALIZED");
			serialized.Setup(x => x.Deserialize(It.IsAny<bool>())).Returns(new Mock<ISourceItem>().Object);
			var version = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			evaluator.EvaluateUpdate(serialized.Object, item.Object);

			serialized.Verify(x => x.Deserialize(false));
		}

		[Test]
		public void EvaluateUpdate_DoesNotDeserialize_WhenDateRevisionNameMatch()
		{
			var evaluator = CreateTestEvaluator();

			var item = new Mock<ISourceItem>();
			var sourceVersion = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");
			item.Setup(x => x.Versions).Returns(new[] { sourceVersion });
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			serialized.Setup(x => x.Deserialize(It.IsAny<bool>())).Returns(new Mock<ISourceItem>().Object);
			var version = CreateTestVersion("en", 1, new DateTime(2013, 1, 1), "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			var result = evaluator.EvaluateUpdate(serialized.Object, item.Object);

			Assert.IsNull(result);
			serialized.Verify(x => x.Deserialize(It.IsAny<bool>()), Times.Never);
		}

		private bool EvaluateUpdate_DateComparisonTest(DateTime sourceModified, DateTime serializedModified)
		{
			var logger = new Mock<ISerializedAsMasterEvaluatorLogger>();

			var evaluator = new SerializedAsMasterEvaluator(logger.Object);

			var item = new Mock<ISourceItem>();
			var sourceVersion = CreateTestVersion("en", 1, sourceModified, "REVISION");
			item.Setup(x => x.Versions).Returns(new[] { sourceVersion });
			item.Setup(x => x.Name).Returns("NAME");

			var serialized = new Mock<ISerializedItem>();
			serialized.Setup(x => x.Name).Returns("NAME");
			serialized.Setup(x => x.Deserialize(It.IsAny<bool>())).Returns(new Mock<ISourceItem>().Object);

			var version = CreateTestVersion("en", 1, serializedModified, "REVISION");

			serialized.Setup(x => x.Versions).Returns(new[] { version });

			evaluator.EvaluateUpdate(serialized.Object, item.Object);

			try
			{
				serialized.Verify(x => x.Deserialize(false));
				return true;
			}
			catch
			{
				return false;
			}
		}

		private SerializedAsMasterEvaluator CreateTestEvaluator()
		{
			var logger = new Mock<ISerializedAsMasterEvaluatorLogger>();

			return new SerializedAsMasterEvaluator(logger.Object);
		}

		internal static ItemVersion CreateTestVersion(string language, int version, DateTime modified, string revision)
		{
			var serializedVersion = new ItemVersion(language, version);
			if (modified != default(DateTime))
				serializedVersion.Fields[FieldIDs.Updated.ToString()] = DateUtil.ToIsoDate(modified);

			if (!string.IsNullOrEmpty(revision))
				serializedVersion.Fields[FieldIDs.Revision.ToString()] = revision;

			return serializedVersion;
		}
	}
}
