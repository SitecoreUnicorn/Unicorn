using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Moq;
using NUnit.Framework;
using Rainbow.Model;
using Rainbow.Storage;
using Sitecore.Data;
using Unicorn.Predicates;

namespace Unicorn.Tests.Predicate
{
	[TestFixture]
	public class SitecorePresetPredicateTests
	{
		private const string ExcludedPath = "/sitecore/layout/Simulators/Android Phone";
		private const string IncludedPath = "/sitecore/layout/Simulators/iPad";
		private const string ExcludedDatabase = "fake";
		private const string IncludedDatabase = "master";
		private static readonly Guid IncludedTemplateId = ID.NewID.Guid;
		private static readonly Guid ExcludedTemplateId = new Guid("{317ADE1D-337A-464A-B7D0-06B4424FC0EA}");
		private static readonly Guid IncludedItemId = ID.NewID.Guid;
		private static readonly Guid ExcludedItemId = new Guid("{E1DC505A-F86F-4C05-B409-AE2246AD3441}");

		[Test]
		public void ctor_ThrowsArgumentNullException_WhenNodeIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationPresetPredicate((XmlNode)null));
		}

		//
		// PATH INCLUSION/EXCLUSION
		//

		[Test]
		public void Includes_ExcludesSerializedItemByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(ExcludedPath.ToUpperInvariant());
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by path failed.");
		}

		[Test]
		public void Includes_ExcludesSerializedItemByPath_WhenCaseDoesNotMatch()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(ExcludedPath);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by path failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(IncludedPath);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by path failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByPath_WhenCaseDoesNotMatch()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(IncludedPath.ToUpperInvariant());
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by path failed.");
		}

		//
		// DATABASE INCLUSION/EXCLUSION
		//

		[Test]
		public void Includes_ExcludesSerializedItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(IncludedPath, database: ExcludedDatabase);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by database failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			// ReSharper disable once RedundantArgumentDefaultValue
			var item = CreateTestItem(IncludedPath, database: IncludedDatabase);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by database failed.");
		}

		//
		// TEMPLATE ID INCLUSION/EXCLUSION
		//

		[Test]
		public void Includes_ExcludesSerializedItemByTemplateId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			// ReSharper disable once RedundantArgumentName
			var item = CreateTestItem(IncludedPath, templateId: ExcludedTemplateId);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by template ID failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByTemplateId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			// ReSharper disable once RedundantArgumentName
			var item = CreateTestItem(IncludedPath, templateId: IncludedTemplateId);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by template ID failed.");
		}

		//
		// ITEM ID INCLUSION/EXCLUSION
		//

		[Test]
		public void Includes_ExcludesSerializedItemByItemId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(IncludedPath, id: ExcludedItemId);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by item ID failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByItemId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(IncludedPath, id: IncludedItemId);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by item ID failed.");
		}

		[Test]
		public void GetRootItems_ReturnsExpectedRootValues()
		{
			var sourceItem1 = new Mock<ISerializableItem>();
			var sourceItem2 = new Mock<ISerializableItem>();

			var sourceDataProvider = new Mock<IDataStore>();
			sourceDataProvider.Setup(x => x.GetByPath("master", "/sitecore/layout/Simulators")).Returns(new[] { sourceItem1.Object });
			sourceDataProvider.Setup(x => x.GetByPath("core", "/sitecore/content")).Returns(new[] { sourceItem2.Object });

			var predicate = new SerializationPresetPredicate(CreateTestConfiguration());

			var roots = predicate.GetRootPaths();

			Assert.IsTrue(roots.Length == 2, "Expected two root paths from test config");
			Assert.AreEqual(roots[0].Database, "master", "Expected first root to be in master db");
			Assert.AreEqual(roots[0].Path, "/sitecore/layout/Simulators", "Expected first root to be /sitecore/layout/Simulators");
			Assert.AreEqual(roots[1].Database, "core", "Expected second root to be in core db");
			Assert.AreEqual(roots[1].Path, "/sitecore/content", "Expected second root to be /sitecore/content");
		}

		private ISerializableItem CreateTestItem(string path, Guid? templateId = null, Guid? id = null, string database = "master")
		{
			var item = new Mock<ISerializableItem>();
			item.SetupGet(x => x.Path).Returns(path);
			item.SetupGet(x => x.Id).Returns(id ?? ID.NewID.Guid);
			item.SetupGet(x => x.TemplateId).Returns(templateId ?? ID.NewID.Guid);
			item.SetupGet(x => x.DatabaseName).Returns(database);

			return item.Object;
		}

		private SerializationPresetPredicate CreateTestPredicate(XmlNode configNode)
		{
			return new SerializationPresetPredicate(configNode);
		}

		private XmlNode CreateTestConfiguration()
		{
			var assembly = Assembly.GetExecutingAssembly();
			string text;
			// ReSharper disable AssignNullToNotNullAttribute
			using (var textStreamReader = new StreamReader(assembly.GetManifestResourceStream("Unicorn.Tests.Predicate.TestConfiguration.xml")))
			// ReSharper restore AssignNullToNotNullAttribute
			{
				text = textStreamReader.ReadToEnd();
			}

			var doc = new XmlDocument();
			doc.LoadXml(text);

			return doc.DocumentElement;
		}
	}
}
