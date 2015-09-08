using NSubstitute;
using NUnit.Framework;
using Rainbow.Tests;
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
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var testItem = new FakeItem(ID.NewID.Guid);

			Assert.IsTrue(testChecker.IsConsistent(testItem));
		}

		[Test]
		public void IsConsistent_ReturnsTrue_WhenNotDuplicated()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var testItem1 = new FakeItem(ID.NewID.Guid);

			var testItem2 = new FakeItem(ID.NewID.Guid);

			testChecker.AddProcessedItem(testItem1);
			Assert.IsTrue(testChecker.IsConsistent(testItem2));
		}

		[Test]
		public void IsConsistent_ReturnsFalse_WhenDuplicated()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = new FakeItem(duplicatedId);

			var testItem2 = new FakeItem(duplicatedId);

			testChecker.AddProcessedItem(testItem1);
			Assert.IsFalse(testChecker.IsConsistent(testItem2));
		}

		[Test]
		public void IsConsistent_ReturnsTrue_WhenDuplicatedIdsAreInDifferentDatabases()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = new FakeItem(duplicatedId, "master");

			var testItem2  = new FakeItem(duplicatedId, "core");

			testChecker.AddProcessedItem(testItem1);
			Assert.IsTrue(testChecker.IsConsistent(testItem2));
		}

		[Test]
		public void IsConsistent_LogsError_WhenDuplicated()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = new FakeItem(duplicatedId);

			var testItem2 = new FakeItem(duplicatedId);

			testChecker.AddProcessedItem(testItem1);
			testChecker.IsConsistent(testItem2);

			testLogger.Received().DuplicateFound(Arg.Any<DuplicateIdConsistencyChecker.DuplicateIdEntry>(), testItem2);
		}
	}
}
