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
	[OutputType(typeof(IItemData))]
	public class ConvertFromRainbowYamlCommand : YamlCommandBase
	{
		protected override void ProcessRecord()
		{
			var yaml = CreateFormatter(CreateFieldFilter());

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Yaml)))
			{
				var item = yaml.ReadSerializedItem(stream, "(from PowerShell)");

				WriteObject(item);
			}
		}

		[Parameter(ValueFromPipeline = true, Mandatory = true)]
		public string Yaml { get; set; }
	}
}