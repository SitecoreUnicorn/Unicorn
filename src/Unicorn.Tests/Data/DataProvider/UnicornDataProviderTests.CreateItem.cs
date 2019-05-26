using NSubstitute;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.FakeDb;
using Unicorn.Data;
using Xunit;

namespace Unicorn.Tests.Data.DataProvider
{
	partial class UnicornDataProviderTests
	{
		public void CreateItem_ShouldNotSerializeItem_WhenSerializationIsDisabled()
		{

		}

		[Theory, AutoDbData]
		public void CreateItem_ShouldNotSerializeItem_WhenPredicateExcludesItem(Db db)
		{
			var target = Substitute.For<ITargetDataStore>();

			using (var provider = CreateTestProvider(db.Database, targetDataStore: target, predicate:CreateExclusiveTestPredicate()))
			{
				var parent = new DbItem("Parent");
				db.Add(parent);

				provider.CreateItem(CreateTestDefinition(), ID.NewID, CreateTestDefinition(id: parent.ID), CreateTestCallContext(db.Database));

				target.DidNotReceive().Save(Arg.Any<IItemData>(), null);
			}
		}

		[Theory, AutoDbData]
		public void CreateItem_ShouldSerializeItem(Db db)
		{
			var target = Substitute.For<ITargetDataStore>();

			using (var provider = CreateTestProvider(db.Database, targetDataStore: target))
			{
				var parent = new DbItem("Parent");
				db.Add(parent);

				provider.CreateItem(CreateTestDefinition(), ID.NewID, CreateTestDefinition(id: parent.ID), CreateTestCallContext(db.Database));

				target.Received().Save(Arg.Any<IItemData>(), null);
			}
		}

		[Theory, AutoDbData]
		public void CreateItem_ShouldSerializeItem_TransparentSyncIsEnabled(Db db)
		{
			var target = Substitute.For<ITargetDataStore>();

			using (var provider = CreateTestProvider(db.Database, targetDataStore: target, enableTransparentSync: true))
			{
				var parent = new DbItem("Parent");
				db.Add(parent);

				provider.CreateItem(CreateTestDefinition(), ID.NewID, CreateTestDefinition(id: parent.ID), CreateTestCallContext(db.Database));

				target.Received().Save(Arg.Any<IItemData>(), null);
			}
		}
	}
}
