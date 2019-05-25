using System;
using System.IO;
using System.Reflection;
using System.Xml;
using FluentAssertions;
using Unicorn.Predicates;
using Unicorn.Predicates.Fields;
using Xunit;

namespace Unicorn.Tests.Predicates
{
	public class FieldTransformsConfigurationTests
	{
		[Fact]
		public void ConfigurationParsingTests()
		{
			var root = CreateTestConfiguration();

			foreach (XmlNode configPredicate in root.ChildNodes)
			{
				var predicate = CreateTestPredicate(configPredicate);

				var manipulator = ((PresetTreeRoot) predicate.GetRootPaths()[0]).FieldValueManipulator;
				var transformers = ((PresetTreeRoot) predicate.GetRootPaths()[0]).FieldValueManipulator.GetFieldValueTransformers();

				switch (predicate.GetRootPaths()[0].Name)
				{
					case "FF 1":
						var fieldTitle = manipulator.GetFieldValueTransformer("Title");
						Assert.NotNull(fieldTitle);
						var fieldText = manipulator.GetFieldValueTransformer("Text");
						Assert.NotNull(fieldText);
						transformers.Length.Should().Be(2);
						break;
					case "FF 2":
						fieldTitle = manipulator.GetFieldValueTransformer("Title");
						Assert.NotNull(fieldTitle);
						fieldText = manipulator.GetFieldValueTransformer("Text");
						Assert.NotNull(fieldText);
						Assert.Equal(FieldTransformDeployRule.LoremIpsumBody, ((MagicTokenTransformer)fieldText).FieldTransformDeployRule);
						transformers.Length.Should().Be(2);
						break;
					case "FF 3":
						fieldTitle = manipulator.GetFieldValueTransformer("Title");
						Assert.NotNull(fieldTitle);
						fieldText = manipulator.GetFieldValueTransformer("Text");
						Assert.NotNull(fieldText);
						var fieldTextBody = manipulator.GetFieldValueTransformer("Text Body");
						Assert.NotNull(fieldTextBody);
						Assert.Equal(FieldTransformDeployRule.Ignore, ((MagicTokenTransformer)fieldTextBody).FieldTransformDeployRule);
						transformers.Length.Should().Be(3);
						break;
					default:
						throw new Exception($"No test case defined for predicate name {predicate.GetRootPaths()[0].Name}");
				}
			}
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
			using (var textStreamReader = new StreamReader(assembly.GetManifestResourceStream("Unicorn.Tests.Predicates.FieldTransformsTestConfiguration.xml")))
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
