using System.IO;
using System.Reflection;
using System.Xml;
using Unicorn.Configuration;

namespace Unicorn.Tests.Configuration
{
	internal class TestXmlConfigurationProvider : XmlConfigurationProvider
	{
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
