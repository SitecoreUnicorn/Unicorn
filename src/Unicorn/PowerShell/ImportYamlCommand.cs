using System.IO;
using System.Management.Automation;
using System.Text;
using Kamsar.WebConsole;
using Rainbow.Storage.Sc.Deserialization;
using Unicorn.Deserialization;
using Unicorn.Logging;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// # RAW YAML DESERIALIZATION
	/// $yaml | Import-Yaml # Deserialize YAML from pipeline into Sitecore
	/// $yaml | Import-Yaml -Raw # Deserialize and disable all field filters
	/// $yamlStringArray | Import-Yaml # Deserialize multiple at once
	/// </summary>
	[Cmdlet("Import", "Yaml")]
	public class ImportYamlCommand : YamlCommandBase
	{
		protected override void ProcessRecord()
		{
			var console = new PowershellProgressStatus(Host, "Deserialize Item");
			var consoleLogger = new WebConsoleLogger(console, MessageType.Debug);

			var yaml = CreateFormatter(CreateFieldFilter());

			var deserializer = new DefaultDeserializer(new DefaultDeserializerLogger(consoleLogger), CreateFieldFilter());

			foreach (var yamlItem in Yaml)
			{
				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(yamlItem)))
				{
					var item = yaml.ReadSerializedItem(stream, "(from PowerShell)");

					consoleLogger.Info(item.Path);
					deserializer.Deserialize(item);
				}
			}
		}

		[Parameter(ValueFromPipeline = true, Mandatory = true)]
		public string[] Yaml { get; set; }
	}
}