using NSubstitute;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.FakeDb;
using Unicorn.Data;
using Xunit;

namespace Unicorn.Tests.Data.DataProvider
{
	partial class UnicornDataProviderTests
	{
		public void SaveItem_ShouldNotSerializeItem_WhenSerializationIsDisabled()
		{

		}

		public void SaveItem_ShouldNotSerializeItem_WhenPredicateExcludesItem()
		{

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
		public void Save_ShouldRenameItem(Db db)
		{
			var target = Substitute.For<ITargetDataStore>();

			using (var provider = CreateTestProvider(db.Database, targetDataStore: target))
			{
				var fieldId = ID.NewID;
				var item = new DbItem("Test") { { fieldId, "World" } };
				db.Add(item);

				var dbItem = db.GetItem(item.ID);

				var changes = new ItemChanges(dbItem);
				changes.Properties.Add("name", new PropertyChange("name", "Test", "Test Item"));

				provider.SaveItem(CreateTestDefinition(), changes, CreateTestCallContext(db.Database));

				target.Received().MoveOrRenameItem(Arg.Any<IItemData>(), "/sitecore/content/Test Item");
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
	}
}
