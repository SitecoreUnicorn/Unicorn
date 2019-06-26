using System.IO;
using System.Reflection;
using System.Xml;
using FluentAssertions;
using Unicorn.Roles.Model;
using Unicorn.Roles.RolePredicates;
using Xunit;

namespace Unicorn.Roles.Tests.RolePredicates
{
	public class ConfigurationRolePredicateTests
	{
		[Theory]
		// Domain tests
		[InlineData(@"allfather\Foo", true)]
		[InlineData(@"AllFather\ZeusDogg", true)]
		// Excluded tests
		[InlineData(@"NOTME\haha", false)]
		[InlineData(@"haha", false)]
		// Pattern tests
		[InlineData(@"some\gonk droid", true)]
		[InlineData(@"some\snorky", true)]
		[InlineData(@"some\fake", false)]
		public void Includes_MatchesExpectedPathResult(string testPath, bool expectedResult)
		{
			var predicate = CreateTestPredicate(CreateTestConfiguration());

			predicate.Includes(new SerializedRoleData(testPath, new string[0], @"C:\fake.yml")).IsIncluded.Should().Be(expectedResult);
		}

		private ConfigurationRolePredicate CreateTestPredicate(XmlNode configNode)
		{
			return new ConfigurationRolePredicate(configNode);
		}

		private XmlNode CreateTestConfiguration()
		{
			var assembly = Assembly.GetExecutingAssembly();
			string text;
			// ReSharper disable AssignNullToNotNullAttribute
			using (var textStreamReader = new StreamReader(assembly.GetManifestResourceStream("Unicorn.Roles.Tests.RolePredicates.TestConfiguration.xml")))
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
