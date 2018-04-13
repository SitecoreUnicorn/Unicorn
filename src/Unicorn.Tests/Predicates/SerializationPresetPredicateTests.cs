using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using FluentAssertions;
using Rainbow.Model;
using Unicorn.Predicates;
using Xunit;

namespace Unicorn.Tests.Predicates
{
	public class SitecorePresetPredicateTests
	{
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
		// INCLUDE NAMEPATTERN test config (rocket|unicorn)
		[InlineData("/includenamepattern/unicorn", true)]
		[InlineData("/includenamepattern/ninja", false)]
		[InlineData("/includenamepattern/ninja unicorn", false)]
		[InlineData("/includenamepattern/rocket", true)]
		[InlineData("/includenamepattern/rocket unicorn", false)]
		// EXPLICIT NO-CHILDREN test config
		[InlineData("/nochildren", true)]
		[InlineData("/nochildren/ignoredchild", false)]
		[InlineData("/nochildren/ignored/stillignored", false)]
		// IMPLICIT NO-CHILDREN test config
		[InlineData("/implicit-nochildren", true)]
		[InlineData("/implicit-nochildren/ignoredchild", false)]
		[InlineData("/implicit-nochildren/ignored/stillignored", false)]
		[InlineData("/implicit-nochildrenwithextrachars", false)]
		// TEMPLATE ID test config
		[InlineData("/sitecore/allowed", true)]
		[InlineData("/sitecore/allowed/child", true)]
		[InlineData("/sitecore/excluded", false)]
		[InlineData("/sitecore/excluded/nochildrenwithexcludedparent", false)]
		// NAME PATTERN test config
		[InlineData("/sitecore/namepattern/foo", true)]
		[InlineData("/sitecore/namepattern/not __Standard values", true)]
		[InlineData("/sitecore/namepattern/__Standard values", false)]
		[InlineData("/sitecore/namepattern/__Standard values/child-thereof", false)]
		// SOME-CHILDREN test config
		[InlineData("/somechildren", true)]
		[InlineData("/somechildren/ignoredchild", false)]
		[InlineData("/somechildren/tests", true)]
		[InlineData("/somechildren/tests/testschild", true)]
		[InlineData("/somechildren/testswithextrachars", false)]
		[InlineData("/somechildren/fests", true)]
		// SOME-CHILDREN ONLY PARENTS test config
		[InlineData("/somechildren-onlyparents", true)]
		[InlineData("/somechildren-onlyparents/onlythis", true)]
		[InlineData("/somechildren-onlyparents/onlythis/notthisdescendant", false)]
		[InlineData("/somechildren-onlyparents/allofthis", true)]
		[InlineData("/somechildren-onlyparents/allofthis/andthischild", true)]
		[InlineData("/somechildren-onlyparents/allbydefault", true)]
		[InlineData("/somechildren-onlyparents/allbydefault/andthischild", true)]
		[InlineData("/somechildren-onlyparents/noneofthis", false)]
		[InlineData("/somechildren-onlyparents/level1", true)]
		[InlineData("/somechildren-onlyparents/level1/level2", true)]
		[InlineData("/somechildren-onlyparents/level1/ignorethis", false)]
		[InlineData("/somechildren-onlyparents/level1/level2/level3", false)]
		// MULTIPLE-EXCLUDES EXCEPTIONS
		[InlineData("/multiple-excludes-except/DK", true)]
		[InlineData("/multiple-excludes-except/DK/SiteData", true)]
		[InlineData("/multiple-excludes-except/DK/SiteData/SiteConfiguration", true)]
		[InlineData("/multiple-excludes-except/DK/SiteData/Widgets", true)]
		[InlineData("/multiple-excludes-except/DK/SiteData/Widgets/ignored-widget", false)]
		[InlineData("/multiple-excludes-except/DK/SiteData/notthis", false)]
		[InlineData("/multiple-excludes-except/DK/Checkout", true)]
		[InlineData("/multiple-excludes-except/DK/Checkout/Basket", true)]
		[InlineData("/multiple-excludes-except/DK/notthis", false)]
		[InlineData("/multiple-excludes-except/SE", true)]
		[InlineData("/multiple-excludes-except/SE/SiteData", true)]
		[InlineData("/multiple-excludes-except/SE/SiteData/SiteConfiguration", true)]
		[InlineData("/multiple-excludes-except/SE/SiteData/Widgets", true)]
		[InlineData("/multiple-excludes-except/SE/SiteData/Widgets/ignored-widget", false)]
		[InlineData("/multiple-excludes-except/SE/SiteData/notthis", false)]
		[InlineData("/multiple-excludes-except/SE/Checkout", true)]
		[InlineData("/multiple-excludes-except/SE/Checkout/Basket", true)]
		[InlineData("/multiple-excludes-except/SE/notthis", false)]
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
		// PATH PREFIX
		[InlineData("/somechildrenofmine", true)]
		[InlineData("/somechildrenofmine/somegrandchild", true)]
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

			roots.Length.Should().Be(17);

			var basicRoot = roots.FirstOrDefault(root => root.Name.Equals("Basic"));
			basicRoot?.DatabaseName.Should().Be("master");
			basicRoot?.Path.Should().Be("/sitecore/layout/Simulators");

			var dbTestRoot = roots.FirstOrDefault(root => root.Name.Equals("DB test"));
			dbTestRoot?.DatabaseName.Should().Be("core");
			dbTestRoot?.Path.Should().Be("/sitecore/coredb");
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

		private IItemData CreateTestItem(string path, string database = "master", string templateId = "{11111111-1111-1111-1111-111111111111}")
		{
			var name = path.Substring(path.LastIndexOf('/') + 1);
			return new ProxyItem { Name = name, Path = path, DatabaseName = database, TemplateId = new Guid(templateId) };
		}
	}
}
