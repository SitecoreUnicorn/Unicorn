using System;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Rainbow.Diff;
using Rainbow.Filtering;
using Rainbow.Model;
using Rainbow.Storage;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.FakeDb;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Logging;

namespace Unicorn.Tests.Evaluator
{
	public class AddOnlyEvaluatorTests
	{
		private readonly Guid _sharedFieldId = Guid.Parse("190f3e37-79fb-4643-9d6b-913fa874ac1f");
		private readonly Guid _unversionedFieldId = Guid.Parse("b67df7dd-9bfe-4138-a9fc-759f6ca6d46a");
		private readonly Guid _versionedFieldId = Guid.Parse("2bbe853c-e6d4-487e-92d8-10f8229ce6b8");
		private readonly CultureInfo _originalLanguage = CultureInfo.InvariantCulture;

		private readonly Guid _newSharedFieldId = Guid.Parse("bdef5a8f-5fb0-44da-9a9f-e98c0aa0ab90");
		private readonly Guid _newUnversionedFieldId = Guid.Parse("65a12662-9712-47a5-a12e-e5f0c492b01b");
		private readonly Guid _newVersionedFieldId = Guid.Parse("4c724204-60ea-48a6-87b8-9ec0d07c9fbd");
		private readonly CultureInfo _newLanguage = CultureInfo.GetCultureInfo("nl-BE");

		[Fact]
		public void EvaluateOrphans_ThrowsArgumentNullException_WhenItemsAreNull()
		{
			var evaluator = CreateTestEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateOrphans(null));
		}

		[Theory, AutoDbData]
		public void EvaluateOrphans_RecyclesOrphanItem(Db db, DbItem dbItem)
		{
			var evaluator = CreateTestEvaluator();

			db.Add(dbItem);
			var item = db.GetItem(dbItem.ID);

			evaluator.EvaluateOrphans(new IItemData[] { new ItemData(item) });

			db.GetItem(item.ID).Should().NotBeNull();
		}

		[Fact]
		public void EvaluateNewSerializedItem_ThrowsArgumentNullException_WhenNewItemIsNull()
		{
			var evaluator = CreateTestEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateNewSerializedItem(null));
		}

		[Theory, AutoDbData]
		public void EvaluateNewSerializedItem_LogsCreatedItem(Db db, DbItem dbItem)
		{
			db.Add(dbItem);
			var item = db.GetItem(dbItem.ID);

			var logger = Substitute.For<IAddOnlyEvaluatorLogger>();
			var evaluator = CreateTestEvaluator(logger);
			var itemData = new ItemData(item);

			evaluator.EvaluateNewSerializedItem(itemData);

			logger.Received().DeserializedNewItem(itemData);
		}

		[Theory, AutoDbData]
		public void EvaluateNewSerializedItem_DeserializesItem(Db db, DbItem dbItem)
		{
			db.Add(dbItem);
			var item = db.GetItem(dbItem.ID);

			var deserializer = Substitute.For<IDeserializer>();
			var evaluator = CreateTestEvaluator(deserializer: deserializer);
			var itemData = new ItemData(item);

			evaluator.EvaluateNewSerializedItem(itemData);

			deserializer.Received().Deserialize(itemData);
		}

		[Fact]
		public void EvaluateUpdate_ThrowsArgumentNullException_WhenSerializedItemIsNull()
		{
			var evaluator = CreateTestEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateUpdate(null, CreateTestItem()));
		}

		[Fact]
		public void EvaluateUpdate_ThrowsArgumentNullException_WhenExistingItemIsNull()
		{
			var evaluator = CreateTestEvaluator();

			Assert.Throws<ArgumentNullException>(() => evaluator.EvaluateUpdate(CreateTestItem(), null));
		}

		[Fact]
		public void EvaluateUpdate_Deserializes_AddedFields()
		{
			var sourceItem = CreateTestItem();
			var targetItem = CreateTestItem();

			targetItem.SharedFields = targetItem.SharedFields.Union(new IItemFieldValue[] { new ProxyFieldValue(_newSharedFieldId, "NewSharedFieldValue") });
			targetItem.UnversionedFields = new IItemLanguage[]
			{
				new ProxyItemLanguage(_originalLanguage)
				{
					Fields = targetItem.UnversionedFields.Single(x => x.Language.Equals(_originalLanguage)).Fields.Union(new IItemFieldValue[] {new ProxyFieldValue(_newUnversionedFieldId, "NewUnversionedFieldValueOrignalLanguage") })
				},
				new ProxyItemLanguage(_newLanguage)
				{
					Fields = new IItemFieldValue[] {new ProxyFieldValue(_newUnversionedFieldId, "NewUnversionedFieldValueNewLanguage")}
				} 
			};
			// Add a new field in a new language, version 1 and a new field to an existing language, version 1, a new version to an existing field in an existing language.
			targetItem.Versions = new IItemVersion[]
			{
				new ProxyItemVersion(_originalLanguage, 2)
				{
					Fields = new IItemFieldValue[] {new ProxyFieldValue(_versionedFieldId, "NewVersionedFieldValueOriginalLanguageNewVersion") }
				},
				new ProxyItemVersion(_originalLanguage, 1)
				{
					Fields = targetItem.Versions.Single(x => x.Language.Equals(_originalLanguage)).Fields.Union(new IItemFieldValue[] {new ProxyFieldValue(_newVersionedFieldId, "NewVersionedFieldValueOriginalLanguage") })
				},
				new ProxyItemVersion(_newLanguage, 1)
				{
					Fields = new IItemFieldValue[] {new ProxyFieldValue(_newVersionedFieldId, "NewVersionedFieldValueNewLanguage") }
				}
			};

			var comparer = Substitute.For<IItemComparer>();
			comparer.FastCompare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(new ItemComparisonResult(sourceItem, targetItem));

			var deserializer = Substitute.For<IDeserializer>();

			var evaluator = CreateTestEvaluator(deserializer: deserializer, comparer: comparer);

			var merged = evaluator.EvaluateUpdate(sourceItem, targetItem);
			merged.Should().NotBeNull();

			// Check shared fields
			Assert.Equal(2, merged.SharedFields.Count());

			Assert.True(merged.SharedFields.Any(x => x.Value.Equals("SharedFieldValue")));
			Assert.True(merged.SharedFields.Any(x => x.Value.Equals("NewSharedFieldValue")));

			// Check unversioned fields
			Assert.Equal(2, merged.UnversionedFields.Count());
			Assert.Equal(2, merged.UnversionedFields.Single(x => x.Language.Equals(_originalLanguage)).Fields.Count());
			Assert.Equal(1, merged.UnversionedFields.Single(x => x.Language.Equals(_newLanguage)).Fields.Count());

			Assert.True(merged.UnversionedFields.Single(x => x.Language.Equals(_originalLanguage)).Fields.Any(x => x.Value.Equals("UnversionedFieldValue")));
			Assert.True(merged.UnversionedFields.Single(x => x.Language.Equals(_originalLanguage)).Fields.Any(x => x.Value.Equals("NewUnversionedFieldValueOrignalLanguage")));
			Assert.True(merged.UnversionedFields.Single(x => x.Language.Equals(_newLanguage)).Fields.Any(x => x.Value.Equals("NewUnversionedFieldValueNewLanguage")));


			// Check versions.
			Assert.Equal(3, merged.Versions.Count());
			Assert.Equal(3, merged.Versions.Where(x => x.Language.Equals(_originalLanguage)).SelectMany(x => x.Fields).Count());
			Assert.Equal(2, merged.Versions.Single(x => x.Language.Equals(_originalLanguage) && x.VersionNumber == 1).Fields.Count());
			Assert.Equal(1, merged.Versions.Single(x => x.Language.Equals(_originalLanguage) && x.VersionNumber == 2).Fields.Count());
			Assert.Equal(1, merged.Versions.Single(x => x.Language.Equals(_newLanguage)).Fields.Count());
			
			Assert.True(merged.Versions.Single(x => x.Language.Equals(_originalLanguage) && x.VersionNumber == 1).Fields.Any(x => x.Value.Equals("VersionedFieldValue")));
			Assert.True(merged.Versions.Single(x => x.Language.Equals(_originalLanguage) && x.VersionNumber == 1).Fields.Any(x => x.Value.Equals("NewVersionedFieldValueOriginalLanguage")));
			Assert.True(merged.Versions.Single(x => x.Language.Equals(_originalLanguage) && x.VersionNumber == 2).Fields.Any(x => x.Value.Equals("NewVersionedFieldValueOriginalLanguageNewVersion")));
			Assert.True(merged.Versions.Single(x => x.Language.Equals(_newLanguage) && x.VersionNumber == 1).Fields.Any(x => x.Value.Equals("NewVersionedFieldValueNewLanguage")));
			
			deserializer.Received().Deserialize(Arg.Any<IItemData>());
		}

		[Fact]
		public void EvaluateUpdate_DoesNotDeserialize_ChangedFields()
		{
			var sourceItem = CreateTestItem();
			var targetItem = CreateTestItem();

			targetItem.SharedFields = new IItemFieldValue[] {new ProxyFieldValue(_sharedFieldId, "ChangedSharedFieldValue")};
			targetItem.UnversionedFields = new IItemLanguage[]
			{
				new ProxyItemLanguage(CultureInfo.InvariantCulture)
				{
					Fields = new IItemFieldValue[] {new ProxyFieldValue(_unversionedFieldId, "ChangedUnversionedFieldValue") }
				}
			};
			targetItem.Versions = new IItemVersion[]
			{
				new ProxyItemVersion(CultureInfo.InvariantCulture, 1)
				{
					Fields = new IItemFieldValue[] {new ProxyFieldValue(_versionedFieldId, "ChangedVersionedFieldValue") }
				}
			};

			var comparer = Substitute.For<IItemComparer>();
			comparer.FastCompare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(new ItemComparisonResult(sourceItem, targetItem));

			var deserializer = Substitute.For<IDeserializer>();

			var evaluator = CreateTestEvaluator(deserializer: deserializer, comparer: comparer);

			evaluator.EvaluateUpdate(sourceItem, targetItem).Should().BeNull();

			deserializer.DidNotReceive().Deserialize(Arg.Any<IItemData>());
		}

		[Fact]
		public void EvaluateUpdate_DoesNotDeserialize_ComparerReturnsBranchChanged()
		{
			var comparer = Substitute.For<IItemComparer>();
			comparer.FastCompare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(new ItemComparisonResult(CreateTestItem(), CreateTestItem(), isBranchChanged: true));

			var deserializer = Substitute.For<IDeserializer>();

			var evaluator = CreateTestEvaluator(deserializer: deserializer, comparer: comparer);

			evaluator.EvaluateUpdate(CreateTestItem(), CreateTestItem()).Should().BeNull();

			deserializer.DidNotReceive().Deserialize(Arg.Any<IItemData>());
		}

		[Fact]
		public void EvaluateUpdate_DoesNotDeserialize_ComparerReturnsTemplateChanged()
		{
			var comparer = Substitute.For<IItemComparer>();
			comparer.FastCompare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(new ItemComparisonResult(CreateTestItem(), CreateTestItem(), isTemplateChanged: true));

			var deserializer = Substitute.For<IDeserializer>();

			var evaluator = CreateTestEvaluator(deserializer: deserializer, comparer: comparer);

			evaluator.EvaluateUpdate(CreateTestItem(), CreateTestItem()).Should().BeNull();

			deserializer.DidNotReceive().Deserialize(Arg.Any<IItemData>());
		}

		[Fact]
		public void EvaluateUpdate_DoesNotDeserialize_ComparerReturnsMoved()
		{
			var comparer = Substitute.For<IItemComparer>();
			comparer.FastCompare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(new ItemComparisonResult(CreateTestItem(), CreateTestItem(), isMoved: true));

			var deserializer = Substitute.For<IDeserializer>();

			var evaluator = CreateTestEvaluator(deserializer: deserializer, comparer: comparer);

			evaluator.EvaluateUpdate(CreateTestItem(), CreateTestItem()).Should().BeNull();

			deserializer.DidNotReceive().Deserialize(Arg.Any<IItemData>());
		}

		[Fact]
		public void EvaluateUpdate_DoesNotDeserialize_ComparerReturnsRenamed()
		{
			var comparer = Substitute.For<IItemComparer>();
			comparer.FastCompare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(new ItemComparisonResult(CreateTestItem(), CreateTestItem(), true));

			var deserializer = Substitute.For<IDeserializer>();

			var evaluator = CreateTestEvaluator(deserializer: deserializer, comparer: comparer);

			evaluator.EvaluateUpdate(CreateTestItem(), CreateTestItem()).Should().BeNull();

			deserializer.DidNotReceive().Deserialize(Arg.Any<IItemData>());
		}

		[Fact]
		public void EvaluateUpdate_DoesNotDeserialize_ComparerReturnsEqual()
		{
			var comparer = Substitute.For<IItemComparer>();
			comparer.FastCompare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(new ItemComparisonResult(CreateTestItem(), CreateTestItem()));

			var deserializer = Substitute.For<IDeserializer>();

			var evaluator = CreateTestEvaluator(deserializer: deserializer, comparer: comparer);

			evaluator.EvaluateUpdate(CreateTestItem(), CreateTestItem()).Should().BeNull();

			deserializer.DidNotReceive().Deserialize(Arg.Any<IItemData>());
		}

		private AddOnlyEvaluator CreateTestEvaluator(IAddOnlyEvaluatorLogger logger = null, IDeserializer deserializer = null, IItemComparer comparer = null)
		{
			if (deserializer == null) deserializer = Substitute.For<IDeserializer>();

			var dataStore = new ConfigurationDataStore(new Lazy<IDataStore>(() => new SitecoreDataStore(deserializer)));

			if (comparer == null)
			{
				comparer = Substitute.For<IItemComparer>();
				comparer.FastCompare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(new ItemComparisonResult(CreateTestItem(), CreateTestItem()));
			}

			return new AddOnlyEvaluator(Substitute.For<ILogger>(), logger ?? Substitute.For<IAddOnlyEvaluatorLogger>(), comparer, CreateTestFieldFilter(), dataStore, Substitute.For<IConfiguration>());
		}

		private IFieldFilter CreateTestFieldFilter()
		{
			var trueFilter = Substitute.For<IFieldFilter>();
			trueFilter.Includes(Arg.Any<Guid>()).Returns(true);

			return trueFilter;
		}

		private ProxyItem CreateTestItem()
		{
			return new ProxyItem
			{
				Path = "/sitecore/content/test",
				SharedFields = new IItemFieldValue[] {new ProxyFieldValue(_sharedFieldId, "SharedFieldValue")},
				UnversionedFields = new IItemLanguage[]
				{
					new ProxyItemLanguage(CultureInfo.InvariantCulture)
					{
						Fields = new IItemFieldValue[] {new ProxyFieldValue(_unversionedFieldId, "UnversionedFieldValue")}
					}
				},
				Versions = new IItemVersion[]
				{
					new ProxyItemVersion(CultureInfo.InvariantCulture, 1)
					{
						Fields = new IItemFieldValue[] {new ProxyFieldValue(_versionedFieldId, "VersionedFieldValue")}
					} 
				}
			};
		}
	}
}
