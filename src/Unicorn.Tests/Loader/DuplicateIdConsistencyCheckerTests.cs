using Moq;
using NUnit.Framework;
using Rainbow.Model;
using Sitecore.Data;
using Unicorn.Loader;

namespace Unicorn.Tests.Loader
{
	[TestFixture]
	public class DuplicateIdConsistencyCheckerTests
	{
		[Test]
		public void IsConsistent_ReturnsTrue_WhenEmpty()
		{
			var testLogger = new Mock<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger.Object);

			var testItem = new Mock<ISerializableItem>();
			testItem.SetupGet(x => x.Id).Returns(() => ID.NewID.Guid);

			Assert.IsTrue(testChecker.IsConsistent(testItem.Object));
		}

		[Test]
		public void IsConsistent_ReturnsTrue_WhenNotDuplicated()
		{
			var testLogger = new Mock<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger.Object);

			var testItem1 = new Mock<ISerializableItem>();
			testItem1.SetupGet(x => x.Id).Returns(() => ID.NewID.Guid);

			var testItem2 = new Mock<ISerializableItem>();
			testItem2.SetupGet(x => x.Id).Returns(() => ID.NewID.Guid);

			testChecker.AddProcessedItem(testItem1.Object);
			Assert.IsTrue(testChecker.IsConsistent(testItem2.Object));
		}

		[Test]
		public void IsConsistent_ReturnsFalse_WhenDuplicated()
		{
			var testLogger = new Mock<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger.Object);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = new Mock<ISerializableItem>();
			testItem1.SetupGet(x => x.Id).Returns(duplicatedId);

			var testItem2 = new Mock<ISerializableItem>();
			testItem2.SetupGet(x => x.Id).Returns(duplicatedId);

			testChecker.AddProcessedItem(testItem1.Object);
			Assert.IsFalse(testChecker.IsConsistent(testItem2.Object));
		}

		[Test]
		public void IsConsistent_ReturnsTrue_WhenDuplicatedIdsAreInDifferentDatabases()
		{
			var testLogger = new Mock<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger.Object);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = new Mock<ISerializableItem>();
			testItem1.SetupGet(x => x.Id).Returns(duplicatedId);
			testItem1.SetupGet(x => x.DatabaseName).Returns("master");

			var testItem2 = new Mock<ISerializableItem>();
			testItem2.SetupGet(x => x.Id).Returns(duplicatedId);
			testItem2.SetupGet(x => x.DatabaseName).Returns("core");

			testChecker.AddProcessedItem(testItem1.Object);
			Assert.IsTrue(testChecker.IsConsistent(testItem2.Object));
		}

		[Test]
		public void IsConsistent_LogsError_WhenDuplicated()
		{
			var testLogger = new Mock<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger.Object);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = new Mock<ISerializableItem>();
			testItem1.SetupGet(x => x.Id).Returns(duplicatedId);

			var testItem2 = new Mock<ISerializableItem>();
			testItem2.SetupGet(x => x.Id).Returns(duplicatedId);

			testChecker.AddProcessedItem(testItem1.Object);
			testChecker.IsConsistent(testItem2.Object);

			testLogger.Verify(x => x.DuplicateFound(testItem1.Object, testItem2.Object), Times.Once());
		}
	}
}
