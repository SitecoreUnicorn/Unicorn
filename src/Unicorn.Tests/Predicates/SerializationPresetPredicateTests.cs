using System;
using System.IO;
using System.Reflection;
using System.Xml;
using FluentAssertions;
using NSubstitute;
using Rainbow.Model;
using Rainbow.Storage;
using Unicorn.Predicates;
using Xunit;

namespace Unicorn.Tests.Predicates
{
	public class SitecorePresetPredicateTests
	{
		private const string ExcludedDatabase = "fake";
		private const string IncludedDatabase = "master";

		[Fact]
		public void ctor_ThrowsArgumentNullException_WhenNodeIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new SerializationPresetPredicate(null, null, null));
		}

		//
		// PATH INCLUSION/EXCLUSION
		//

		[Theory]
		// BASIC test config
		[InlineData("/sitecore/layout/Simulators/iPad", true)]
		[InlineData("/sitecore/layout/Simulators/Android Phone", false)]
		[InlineData("/sitecore/layout/Simulators/iPhone", false)]
		[InlineData("/sitecore/layout/Simulators/iPhone Apps", true)] // path starts with excluded iPhone path but is not equal
		[InlineData("/sitecore/layout/Simulators/iPhone Apps/1.0", false)]
		// EXPLICIT NO-CHILDREN test config
		[InlineData("/nochildren", true)]
		[InlineData("/nochildren/ignoredchild", false)]
		[InlineData("/nochildren/ignored/stillignored", false)]
		// IMPLICIT NO-CHILDREN test config
		[InlineData("/implicit-nochildren", true)]
		[InlineData("/implicit-nochildren/ignoredchild", false)]
		[InlineData("/implicit-nochildren/ignored/stillignored", false)]
		[InlineData("/implicit-nochildrenwithextrachars", true)]
		// SOME-CHILDREN test config
		[InlineData("/somechildren", true)]
		[InlineData("/somechildren/ignoredchild", false)]
		[InlineData("/somechildren/tests", true)]
		[InlineData("/somechildren/tests/testschild", true)]
		[InlineData("/somechildren/testswithextrachars", false)]
		[InlineData("/somechildren/fests", true)]
		// CHILDREN-OF-CHILDREN test config
		[InlineData("/CoC", true)]
		[InlineData("/CoC/stuff", true)]
		[InlineData("/CoC/stuff/child", false)]
		[InlineData("/CoC/stuffwithextrachars", true)]
		[InlineData("/CoC/morestuff", true)]
		[InlineData("/CoC/morestuff/child/exclusion", false)]
		[InlineData("/CoC/yetmorestuff", true)]
		[InlineData("/CoC/yetmorestuff/gorilla", false)]
		[InlineData("/CoC/yetmorestuff/monkey", true)]
		// WILDCARD CHILDREN-OF-CHILREN test config
		[InlineData("/Wild", true)]
		[InlineData("/Wild/Wild Woozles", true)]
		[InlineData("/Wild/MikesBeers", true)]
		[InlineData("/Wild/MikesBeers/Unopened", false)]
		// WILDCARD CHILDREN-OF-CHILREN subitem test config
		[InlineData("/ChildWild", true)]
		[InlineData("/ChildWild/Wild Woozles", true)]
		[InlineData("/ChildWild/Mike", true)]
		[InlineData("/ChildWild/Mike/Fridge", true)]
		[InlineData("/ChildWild/Mike/Fridge/Beers", false)]
		// LITERAL WILDCARDS (root/child)
		[InlineData("/LiteralWild", false)]
		[InlineData("/LiteralWild/*", true)]
		[InlineData("/LiteralWild/*/Foo", false)]
		[InlineData("/LiteralWild/*/*", true)]
		public void Includes_MatchesExpectedPathResult(string testPath, bool expectedResult)
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(testPath);

			// all test cases should handle mismatching path casing so we use uppercase paths too as a check on that
			var mismatchedCasePathItem = CreateTestItem(testPath.ToUpperInvariant());

			predicate.Includes(item).IsIncluded.Should().Be(expectedResult);
			predicate.Includes(mismatchedCasePathItem).IsIncluded.Should().Be(expectedResult);
		}

		//
		// DATABASE INCLUSION/EXCLUSION
		//

		[Theory]
		// DB TEST test config
		[InlineData("/sitecore/coredb", "core", true)]
		[InlineData("/sitecore/coredb", "master", false)]
		public void Includes_MatchesExpectedDatabaseResult(string testPath, string databaseName, bool expectedResult)
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			var item = CreateTestItem(testPath, databaseName);
			var mismatchedDbCaseTestItem = CreateTestItem(testPath, databaseName.ToUpperInvariant());

			predicate.Includes(item).IsIncluded.Should().Be(expectedResult);
			predicate.Includes(mismatchedDbCaseTestItem).IsIncluded.Should().Be(expectedResult);
		}

		[Fact]
		// Deps: BASIC and DB TEST test configs
		public void GetRootItems_ReturnsExpectedRootValues()
		{
			var predicate = new SerializationPresetPredicate(CreateTestConfiguration(), null, null);

			var roots = predicate.GetRootPaths();

			roots.Length.Should().Be(10);
			roots[0].DatabaseName.Should().Be("master");
			roots[0].Path.Should().Be("/sitecore/layout/Simulators");
			roots[7].DatabaseName.Should().Be("core");
			roots[7].Path.Should().Be("/sitecore/coredb");
		}

		private SerializationPresetPredicate CreateTestPredicate(XmlNode configNode)
		{
			return new SerializationPresetPredicate(configNode, null, null);
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

		private IItemData CreateTestItem(string path, string database = "master")
		{
			return new ProxyItem { Path = path, DatabaseName = database };
		}
	}
}
