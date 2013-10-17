using System.Linq;
using System;
using System.IO;
using System.Reflection;
using System.Xml;
using Moq;
using NUnit.Framework;
using Sitecore.Data;
using Unicorn.Predicates;
using Unicorn.Serialization;
using Unicorn.Data;

namespace Unicorn.Tests.Predicate
{
	[TestFixture]
	public class SitecorePresetPredicateTests
	{
		private const string ExcludedPath = "/sitecore/layout/Simulators/Android Phone";
		private const string IncludedPath = "/sitecore/layout/Simulators/iPad";
		private const string ExcludedDatabase = "fake";
		private const string IncludedDatabase = "master";
		private const string IncludedTemplateName = "Simulator";
		private const string ExcludedTemplateName = "No Silverlight Support Trait";
		private static readonly ID IncludedTemplateId = ID.NewID;
		private static readonly ID ExcludedTemplateId = new ID("{317ADE1D-337A-464A-B7D0-06B4424FC0EA}");
		private static readonly ID IncludedItemId = ID.NewID;
		private static readonly ID ExcludedItemId = new ID("{E1DC505A-F86F-4C05-B409-AE2246AD3441}");

		[Test]
		public void ctor_ThrowsArgumentNullException_WhenPresetIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationPresetPredicate((string)null));
		}

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

			var item = CreateTestSerializedItem(ExcludedPath);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by path failed.");
		}

		[Test]
		public void Includes_ExcludesSerializedReferenceByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedReference(ExcludedPath);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized reference by path failed.");
		}

		[Test]
		public void Includes_ExcludesSourceItemByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(ExcludedPath);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude source item by path failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedItem(IncludedPath);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by path failed.");
		}

		[Test]
		public void Includes_IncludesSerializedReferenceByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedReference(IncludedPath);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized reference by path failed.");
		}

		[Test]
		public void Includes_IncludesSourceItemByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(IncludedPath);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include source item by path failed.");
		}

		//
		// DATABASE INCLUSION/EXCLUSION
		//

		[Test]
		public void Includes_ExcludesSerializedItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedItem(IncludedPath, database: ExcludedDatabase);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by database failed.");
		}

		[Test]
		public void Includes_ExcludesSerializedReferenceByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedReference(IncludedPath, database: ExcludedDatabase);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized reference by database failed.");
		}

		[Test]
		public void Includes_ExcludesSourceItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(IncludedPath, database: ExcludedDatabase);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude source item by database failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

// ReSharper disable RedundantArgumentDefaultValue
			var item = CreateTestSerializedItem(IncludedPath, database: IncludedDatabase);
// ReSharper restore RedundantArgumentDefaultValue
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by database failed.");
		}

		[Test]
		public void Includes_IncludesSerializedReferenceByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

// ReSharper disable RedundantArgumentDefaultValue
			var item = CreateTestSerializedReference(IncludedPath, database: IncludedDatabase);
// ReSharper restore RedundantArgumentDefaultValue
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized reference by database failed.");
		}

		[Test]
		public void Includes_IncludesSourceItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

// ReSharper disable RedundantArgumentDefaultValue
			var item = CreateTestSourceItem(IncludedPath, database: IncludedDatabase);
// ReSharper restore RedundantArgumentDefaultValue
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include source item by database failed.");
		}

		//
		// TEMPLATE NAME INCLUSION/EXCLUSION
		//

		[Test]
		public void Includes_ExcludesSerializedItemByTemplateName()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedItem(IncludedPath, template: ExcludedTemplateName);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by template name failed.");
		}

		[Test]
		public void Includes_ExcludesSourceItemByTemplateName()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(IncludedPath, template: ExcludedTemplateName);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude source item by template name failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByTemplateName()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedItem(IncludedPath, template: IncludedTemplateName);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by template name failed.");
		}

		[Test]
		public void Includes_IncludesSourceItemByTemplateName()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(IncludedPath, template: IncludedTemplateName);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include source item by template name failed.");
		}

		//
		// TEMPLATE ID INCLUSION/EXCLUSION
		//

		[Test]
		public void Includes_ExcludesSerializedItemByTemplateId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedItem(IncludedPath, templateId: ExcludedTemplateId);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by template ID failed.");
		}

		[Test]
		public void Includes_ExcludesSourceItemByTemplateId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(IncludedPath, templateId: ExcludedTemplateId);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude source item by template ID failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByTemplateId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedItem(IncludedPath, templateId: IncludedTemplateId);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by template ID failed.");
		}

		[Test]
		public void Includes_IncludesSourceItemByTemplateId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(IncludedPath, templateId: IncludedTemplateId);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include source item by template ID failed.");
		}

		//
		// ITEM ID INCLUSION/EXCLUSION
		//

		[Test]
		public void Includes_ExcludesSerializedItemByItemId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedItem(IncludedPath, id: ExcludedItemId);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by item ID failed.");
		}

		[Test]
		public void Includes_ExcludesSourceItemByItemId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(IncludedPath, id: ExcludedItemId);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude source item by item ID failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByItemId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSerializedItem(IncludedPath, id: IncludedItemId);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by item ID failed.");
		}

		[Test]
		public void Includes_IncludesSourceItemByItemId()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestSourceItem(IncludedPath, id: IncludedItemId);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include source item by item ID failed.");
		}

		private ISourceItem CreateTestSourceItem(string path, ID templateId = null, string template = "Test", ID id = null, string database = "master")
		{
			var item = new Mock<ISourceItem>();
			item.SetupGet(x => x.ItemPath).Returns(path);
			item.SetupGet(x => x.Id).Returns(id ?? ID.NewID);
			item.SetupGet(x => x.TemplateName).Returns(template);
			item.SetupGet(x => x.TemplateId).Returns(templateId ?? ID.NewID);
			item.SetupGet(x => x.DatabaseName).Returns(database);

			return item.Object;
		}

		private ISerializedItem CreateTestSerializedItem(string path, ID templateId = null, string template = "Test", ID id = null, string database = "master")
		{
			var item = new Mock<ISerializedItem>();
			item.SetupGet(x => x.ItemPath).Returns(path);
			item.SetupGet(x => x.Id).Returns(id ?? ID.NewID);
			item.SetupGet(x => x.TemplateName).Returns(template);
			item.SetupGet(x => x.TemplateId).Returns(templateId ?? ID.NewID);
			item.SetupGet(x => x.DatabaseName).Returns(database);

			return item.Object;
		}

		private ISerializedReference CreateTestSerializedReference(string path, string database = "master")
		{
			var item = new Mock<ISerializedReference>();
			item.SetupGet(x => x.ItemPath).Returns(path);
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
