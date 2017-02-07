using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using Rainbow.Model;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// # RAW YAML READING
	/// $yaml | ConvertFrom-RainbowYaml # Read IItemData from YAML
	/// $yamlStringArray | Import-RainbowYaml # Read multiple IItemData at once
	/// </summary>
	[Cmdlet("ConvertFrom", "RainbowYaml")]
	public class ConvertFromRainbowYamlCommand : YamlCommandBase
	{
		protected override void ProcessRecord()
		{
			var yaml = CreateFormatter(CreateFieldFilter());

			var results = new List<IItemData>(Yaml.Length);

			foreach (var yamlItem in Yaml)
			{
				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(yamlItem)))
				{
					var item = yaml.ReadSerializedItem(stream, "(from PowerShell)");

					results.Add(item);
				}
			}

			WriteObject(results);
		}

		[Parameter(ValueFromPipeline = true, Mandatory = true)]
		public string[] Yaml { get; set; }
	}
}