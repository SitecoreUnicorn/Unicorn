using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets;
using Cognifide.PowerShell.Core.Validation;
using Unicorn.Configuration;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// Get-UnicornConfiguration "Foundation.Foo" # Get one
	/// Get-UnicornConfiguration "Foundation.*" # Get by filter
	/// Get-UnicornConfiguration # Get all
	/// </summary>
	[OutputType(typeof(IConfiguration)), Cmdlet("Get", "UnicornConfiguration")]
	public class GetUnicornConfigurationCommand : BaseCommand
	{
		public static string[] Autocomplete = UnicornConfigurationManager.Configurations.Select(cfg => cfg.Name).ToArray();

		protected override void ProcessRecord()
		{
			var configs = UnicornConfigurationManager.Configurations;

			if (!string.IsNullOrWhiteSpace(Filter))
			{
				configs = WildcardFilter(Filter, configs, configuration => configuration.Name).ToArray();
			}

			foreach (var config in configs)
			{
				WriteObject(config);
			}
		}

		[Parameter(Position = 0), AutocompleteSet("Autocomplete")]
		public string Filter { get; set; }
	}
}