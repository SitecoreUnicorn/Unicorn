using System;
using NSubstitute;
using Rainbow.Model;
using Xunit;
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

			var testItem = CreateTestItem(ID.NewID.Guid);

			Assert.True(testChecker.IsConsistent(testItem));
		}

		[Fact]
		public void IsConsistent_ReturnsTrue_WhenNotDuplicated()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var testItem1 = CreateTestItem(ID.NewID.Guid);

			var testItem2 = CreateTestItem(ID.NewID.Guid);

			testChecker.AddProcessedItem(testItem1);
			Assert.True(testChecker.IsConsistent(testItem2));
		}

		[Fact]
		public void IsConsistent_ReturnsFalse_WhenDuplicated()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = CreateTestItem(duplicatedId);

			var testItem2 = CreateTestItem(duplicatedId);

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
			var testItem1 = CreateTestItem(duplicatedId, "master");

			var testItem2 = CreateTestItem(duplicatedId, "core");

			testChecker.AddProcessedItem(testItem1);
			Assert.True(testChecker.IsConsistent(testItem2));
		}

		[Fact]
		public void IsConsistent_LogsError_WhenDuplicated()
		{
			var testLogger = Substitute.For<IDuplicateIdConsistencyCheckerLogger>();

			var testChecker = new DuplicateIdConsistencyChecker(testLogger);

			var duplicatedId = ID.NewID.Guid;

			var testItem1 = CreateTestItem(duplicatedId);

			var testItem2 = CreateTestItem(duplicatedId);

			testChecker.AddProcessedItem(testItem1);
			testChecker.IsConsistent(testItem2);

			testLogger.Received().DuplicateFound(Arg.Any<DuplicateIdConsistencyChecker.DuplicateIdEntry>(), testItem2);
		}

		private IItemData CreateTestItem(Guid id, string database = "master")
		{
			return new ProxyItem { Id = id, Path = "/sitecore/test", DatabaseName = database };
		}
	}
}
