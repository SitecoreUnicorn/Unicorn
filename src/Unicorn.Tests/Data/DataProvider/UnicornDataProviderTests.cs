using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Rainbow.Filtering;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.FakeDb;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Predicates;
using Xunit;

namespace Unicorn.Tests.Data.DataProvider
{
	public partial class UnicornDataProviderTests
	{
		[Theory, AutoDbData]
		public void ShouldDisableTransparentSync_WhenDisablerInScope(Db db)
		{
			using (var provider = CreateTestProvider(db.Database, enableTransparentSync: true))
			{
				provider.DisableTransparentSync.Should().BeFalse();

				using (new TransparentSyncDisabler())
				{
					provider.DisableTransparentSync.Should().BeTrue();
				}

				provider.DisableTransparentSync.Should().BeFalse();
			}
		}





		// TODO

		private UnicornDataProvider CreateTestProvider(Database db, ITargetDataStore targetDataStore = null, ISourceDataStore sourceDataStore = null, IPredicate predicate = null, IFieldFilter filter = null, IUnicornDataProviderLogger logger = null, bool enableTransparentSync = false)
		{
			if (predicate == null)
			{
				predicate = CreateInclusiveTestPredicate();
			}

			if (filter == null)
			{
				filter = Substitute.For<IFieldFilter>();
				filter.Includes(Arg.Any<Guid>()).Returns(true);
			}

			var dp = new UnicornDataProvider(targetDataStore ?? Substitute.For<ITargetDataStore>(), sourceDataStore ?? Substitute.For<ISourceDataStore>(), predicate, filter, logger ?? Substitute.For<IUnicornDataProviderLogger>(), new DefaultUnicornDataProviderConfiguration(enableTransparentSync));

			dp.ParentDataProvider = db.GetDataProviders().First();

			return dp;
		}

		private ItemDefinition CreateTestDefinition(ID id = null, string name = null, ID templateId = null, ID branchId = null)
		{
			return new ItemDefinition(id ?? ID.NewID, name ?? "Test", templateId ?? ID.NewID, branchId ?? ID.NewID);
		}

		private CallContext CreateTestCallContext(Database db)
		{
			return new CallContext(new DataManager(db), 1);
		}

		private IPredicate CreateInclusiveTestPredicate()
		{
			var predicate = Substitute.For<IPredicate>();
			predicate.Includes(Arg.Any<IItemData>()).Returns(new PredicateResult(true));

			return predicate;
		}

		private IPredicate CreateExclusiveTestPredicate()
		{
			var predicate = Substitute.For<IPredicate>();
			predicate.Includes(Arg.Any<IItemData>()).Returns(new PredicateResult(false));

			return predicate;
		}
	}
}
