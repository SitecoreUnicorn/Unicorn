using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets;
using Unicorn.Configuration;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// Get-UnicornConfiguration "Foundation.Foo" # Get one
	/// Get-UnicornConfiguration "Foundation.*" # Get by filter
	/// Get-UnicornConfiguration # Get all
	/// </summary>
	[OutputType(typeof(IConfiguration[])), Cmdlet("Get", "UnicornConfiguration")]
	public class GetUnicornConfigurationCommand : BaseCommand
	{
		protected override void ProcessRecord()
		{
			var configs = UnicornConfigurationManager.Configurations;

			if (!string.IsNullOrWhiteSpace(Filter))
			{
				configs = WildcardFilter(Filter, configs, configuration => configuration.Name).ToArray();
			}

			WriteObject(configs);
		}

		[Parameter(Position = 0)]
		public string Filter { get; set; }
	}
}