using System;
using System.IO;
using System.Reflection;
using System.Xml;
using NSubstitute;
using NUnit.Framework;
using Rainbow.Model;
using Rainbow.Storage;
using Rainbow.Tests;
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

			var item = new FakeItem(path:ExcludedPath);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by path failed.");
		}

		[Test]
		public void Includes_ExcludesSerializedItemByPath_WhenCaseDoesNotMatch()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: ExcludedPath.ToUpperInvariant());
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by path failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: IncludedPath);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by path failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByPath_WhenCaseDoesNotMatch()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: IncludedPath.ToUpperInvariant());
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

			var item = new FakeItem(path: IncludedPath, databaseName: ExcludedDatabase);
			var includes = predicate.Includes(item);

			Assert.IsFalse(includes.IsIncluded, "Exclude serialized item by database failed.");
		}

		[Test]
		public void Includes_IncludesSerializedItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			// ReSharper disable once RedundantArgumentDefaultValue
			var item = new FakeItem(path:IncludedPath, databaseName: IncludedDatabase);
			var includes = predicate.Includes(item);

			Assert.IsTrue(includes.IsIncluded, "Include serialized item by database failed.");
		}

		[Test]
		public void GetRootItems_ReturnsExpectedRootValues()
		{
			var sourceItem1 = new FakeItem();
			var sourceItem2 = new FakeItem();

			var sourceDataProvider = Substitute.For<IDataStore>();
			sourceDataProvider.GetByPath("master", "/sitecore/layout/Simulators").Returns(new[] { sourceItem1 });
			sourceDataProvider.GetByPath("core", "/sitecore/content").Returns(new[] { sourceItem2 });

			var predicate = new SerializationPresetPredicate(CreateTestConfiguration());

			var roots = predicate.GetRootPaths();

			Assert.IsTrue(roots.Length == 2, "Expected two root paths from test config");
			Assert.AreEqual(roots[0].DatabaseName, "master", "Expected first root to be in master db");
			Assert.AreEqual(roots[0].Path, "/sitecore/layout/Simulators", "Expected first root to be /sitecore/layout/Simulators");
			Assert.AreEqual(roots[1].DatabaseName, "core", "Expected second root to be in core db");
			Assert.AreEqual(roots[1].Path, "/sitecore/content", "Expected second root to be /sitecore/content");
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
