using NSubstitute;
using Xunit;
using Rainbow.Tests;
using Sitecore.Data;
using Unicorn.Loader;

namespace Unicorn.Tests.Loader
{
	public class DuplicateIdConsistencyCheckerTests
	{
		[Fact]
		public void IsConsistent_ReturnsTrue_WhenEmpty()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var testItem = new FakeItem(ID.NewID.Guid);

			Assert.True(testChecker.IsConsistent(testItem));
		}

		[Fact]
		public void IsConsistent_ReturnsTrue_WhenNotDuplicated()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var testItem1 = new FakeItem(ID.NewID.Guid);

			var testItem2 = new FakeItem(ID.NewID.Guid);

			testChecker.AddProcessedItem(testItem1);
			Assert.True(testChecker.IsConsistent(testItem2));
		}

		[Fact]
		public void IsConsistent_ReturnsFalse_WhenDuplicated()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = new FakeItem(duplicatedId);

			var testItem2 = new FakeItem(duplicatedId);

			testChecker.AddProcessedItem(testItem1);
			Assert.False(testChecker.IsConsistent(testItem2));
		}

		[Fact]
		public void IsConsistent_ReturnsTrue_WhenDuplicatedIdsAreInDifferentDatabases()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var duplicatedId = ID.NewID.Guid;

			// ReSharper disable once RedundantArgumentDefaultValue
			var testItem1 = new FakeItem(duplicatedId, "master");

			var testItem2  = new FakeItem(duplicatedId, "core");

			testChecker.AddProcessedItem(testItem1);
			Assert.True(testChecker.IsConsistent(testItem2));
		}

		[Fact]
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
