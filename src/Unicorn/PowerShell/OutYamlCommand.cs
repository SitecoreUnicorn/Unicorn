using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Sitecore.Data.Items;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// # RAW YAML OUTPUT
	/// Get-Item "/sitecore/content" | Out-Yaml # Convert an item to YAML format (always uses default excludes and field formatters)
	/// Get-ChildItem "/sitecore/content" | Out-Yaml # Convert many items to YAML strings
	/// Get-Item "/sitecore/content" | Out-Yaml -Raw # Disable all field formats and field filtering
	/// </summary>
	[OutputType(typeof(string)), Cmdlet("Out", "Yaml")]
	public class OutYamlCommand : YamlCommandBase
	{
		protected override void ProcessRecord()
		{
			var items = ItemData ?? Item?.Select(item => new ItemData(item)).ToArray();

			if(items == null) throw new InvalidOperationException("-Item and -ItemData were both not set. Pass one, or send an item in from the pipeline.");

			var yaml = CreateFormatter(CreateFieldFilter());

			foreach (var item in items)
			{
				using (var stream = new MemoryStream())
				{
					yaml.WriteSerializedItem(item, stream);

					stream.Seek(0, SeekOrigin.Begin);

					using (var reader = new StreamReader(stream))
					{
						WriteObject(reader.ReadToEnd());
					}
				}
			}
		}

		[Parameter(ValueFromPipeline = true)]
		public IItemData[] ItemData { get; set; }

		[Parameter(ValueFromPipeline = true, Position = 0)]
		public Item[] Item { get; set; }
	}
}