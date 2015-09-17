using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Rainbow.Filtering;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.FakeDb;
using Sitecore.FakeDb.Data.DataProviders;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Predicates;
using Xunit;

namespace Unicorn.Tests.Data.DataProvider
{
	public class UnicornDataProviderTests
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

		[Theory, AutoDbData]
		public void Create_ShouldSerializeItem(Db db)
		{
			var target = Substitute.For<ITargetDataStore>();

			using (var provider = CreateTestProvider(db.Database, targetDataStore: target))
			{
				provider.CreateItem(CreateTestDefinition(), ID.NewID, CreateTestDefinition(), CreateTestCallContext(db.Database));

				target.Received().Save(Arg.Any<IItemData>());
			}
		}

		[Theory, AutoDbData]
		public void Save_ShouldSerializeItem(Db db)
		{
			var target = Substitute.For<ITargetDataStore>();

			using (var provider = CreateTestProvider(db.Database, targetDataStore: target))
			{
				var fieldId = ID.NewID;
				var item = new DbItem("Test") { { fieldId, "World" } };
				db.Add(item);

				var dbItem = db.GetItem(item.ID);

				var changes = new ItemChanges(dbItem);
				changes.SetFieldValue(dbItem.Fields[fieldId], "Hello", "World");

				provider.SaveItem(CreateTestDefinition(), changes, CreateTestCallContext(db.Database));

				target.Received().Save(Arg.Any<IItemData>());
			}
		}

		[Theory, AutoDbData]
		public void Save_ShouldNotSerializeItem_IfNoMeaningfulChanges(Db db, DbItem item)
		{
			var target = Substitute.For<ITargetDataStore>();

			using (var provider = CreateTestProvider(db.Database, targetDataStore: target))
			{
				db.Add(item);

				var dbItem = db.GetItem(item.ID);
				var changes = new ItemChanges(dbItem);

				provider.SaveItem(CreateTestDefinition(), changes, CreateTestCallContext(db.Database));

				target.DidNotReceive().Save(Arg.Any<IItemData>());
			}
		}

		// TODO

		private UnicornDataProvider CreateTestProvider(Database db, ITargetDataStore targetDataStore = null, ISourceDataStore sourceDataStore = null, IPredicate predicate = null, IFieldFilter filter = null, IUnicornDataProviderLogger logger = null, bool enableTransparentSync = false)
		{
			if (predicate == null)
			{
				predicate = Substitute.For<IPredicate>();
				predicate.Includes(Arg.Any<IItemData>()).Returns(new PredicateResult(true));
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

		private ItemDefinition CreateTestDefinition()
		{
			return new ItemDefinition(ID.NewID, "Test", ID.NewID, ID.NewID);
		}

		private CallContext CreateTestCallContext(Database db)
		{
			return new CallContext(new DataManager(db), 1);
		}
	}
}
