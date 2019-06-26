using System.IO;
using System.Reflection;
using System.Xml;
using Unicorn.Configuration;
using Unicorn.Pipelines.UnicornExpandConfigurationVariables;

namespace Unicorn.Tests.Configuration
{
	internal class TestConfigyConfigurationProvider : ConfigyConfigurationProvider
	{
		public TestConfigyConfigurationProvider() : base(new HelixConventionVariablesReplacer())
		{
			
		}

		protected override XmlNode GetConfigurationNode()
		{
			var assembly = Assembly.GetExecutingAssembly();
			string text;
			// ReSharper disable AssignNullToNotNullAttribute
			using (var textStreamReader = new StreamReader(assembly.GetManifestResourceStream("Unicorn.Tests.Configuration.TestXmlConfiguration.xml")))
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
