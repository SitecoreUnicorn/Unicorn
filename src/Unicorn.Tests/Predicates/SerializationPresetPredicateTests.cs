using System;
using System.IO;
using System.Reflection;
using System.Xml;
using NSubstitute;
using Rainbow.Storage;
using Rainbow.Tests;
using Unicorn.Predicates;
using Xunit;

namespace Unicorn.Tests.Predicates
{
	public class SitecorePresetPredicateTests
	{
		private const string ExcludedPath = "/sitecore/layout/Simulators/Android Phone";
		private const string IncludedPath = "/sitecore/layout/Simulators/iPad";
		private const string ExcludedDatabase = "fake";
		private const string IncludedDatabase = "master";

		[Fact]
		public void ctor_ThrowsArgumentNullException_WhenNodeIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationPresetPredicate((XmlNode)null));
		}

		//
		// PATH INCLUSION/EXCLUSION
		//

		[Fact]
		public void Includes_ExcludesSerializedItemByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path:ExcludedPath);
			var includes = predicate.Includes(item);

			Assert.False(includes.IsIncluded, "Exclude serialized item by path failed.");
		}

		[Fact]
		public void Includes_ExcludesSerializedItemByPath_WhenCaseDoesNotMatch()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: ExcludedPath.ToUpperInvariant());
			var includes = predicate.Includes(item);

			Assert.False(includes.IsIncluded, "Exclude serialized item by path failed.");
		}

		[Fact]
		public void Includes_IncludesSerializedItemByPath_WhenChildrenOfRootAreExcluded_AndPathIsRootItem()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: "/test");
			var includes = predicate.Includes(item);

			Assert.True(includes.IsIncluded, "Included parent serialized item when all children excluded failed.");
		}

		[Fact]
		public void Includes_ExcludesSerializedItemByPath_WhenChildrenOfRootAreExcluded()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: "/test/child");
			var includes = predicate.Includes(item);

			Assert.False(includes.IsIncluded, "Exclude serialized item by all children failed.");
		}

		[Fact]
		public void Includes_IncludesSerializedItemByPath()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: IncludedPath);
			var includes = predicate.Includes(item);

			Assert.True(includes.IsIncluded, "Include serialized item by path failed.");
		}

		[Fact]
		public void Includes_IncludesSerializedItemByPath_WhenCaseDoesNotMatch()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: IncludedPath.ToUpperInvariant());
			var includes = predicate.Includes(item);

			Assert.True(includes.IsIncluded, "Include serialized item by path failed.");
		}

		//
		// DATABASE INCLUSION/EXCLUSION
		//

		[Fact]
		public void Includes_ExcludesSerializedItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = new FakeItem(path: IncludedPath, databaseName: ExcludedDatabase);
			var includes = predicate.Includes(item);

			Assert.False(includes.IsIncluded, "Exclude serialized item by database failed.");
		}

		[Fact]
		public void Includes_IncludesSerializedItemByDatabase()
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			// ReSharper disable once RedundantArgumentDefaultValue
			var item = new FakeItem(path:IncludedPath, databaseName: IncludedDatabase);
			var includes = predicate.Includes(item);

			Assert.True(includes.IsIncluded, "Include serialized item by database failed.");
		}

		[Fact]
		public void GetRootItems_ReturnsExpectedRootValues()
		{
			var sourceItem1 = new FakeItem();
			var sourceItem2 = new FakeItem();

			var sourceDataProvider = Substitute.For<IDataStore>();
			sourceDataProvider.GetByPath("master", "/sitecore/layout/Simulators").Returns(new[] { sourceItem1 });
			sourceDataProvider.GetByPath("core", "/sitecore/content").Returns(new[] { sourceItem2 });

			var predicate = new SerializationPresetPredicate(CreateTestConfiguration());

			var roots = predicate.GetRootPaths();

			Assert.True(roots.Length == 3, "Expected three root paths from test config");
			Assert.Equal(roots[0].DatabaseName, "master");
			Assert.Equal(roots[0].Path, "/sitecore/layout/Simulators");
			Assert.Equal(roots[1].DatabaseName, "core");
			Assert.Equal(roots[1].Path, "/sitecore/content");
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
			using (var textStreamReader = new StreamReader(assembly.GetManifestResourceStream("Unicorn.Tests.Predicates.TestConfiguration.xml")))
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
