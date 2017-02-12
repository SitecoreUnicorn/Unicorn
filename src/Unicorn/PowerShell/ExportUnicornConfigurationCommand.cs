using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets;
using Kamsar.WebConsole;
using Unicorn.Configuration;
using Unicorn.Logging;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// Export-UnicornConfiguration "Foundation.Foo" # Reserialize one
	/// Export-UnicornConfiguration @("Foundation.Foo", "Foundation.Bar") # Reserialize multiple by name
	/// Get-UnicornConfiguration "Foundation.*" | Export-UnicornConfiguration # Reserialize from pipeline
	/// Export-UnicornConfiguration -All -SkipTransparent -LogLevel Warn # Optionally skip Transparent Sync configs, set log output level
	/// </summary>
	[Cmdlet("Export", "UnicornConfiguration")]
	public class ExportUnicornConfigurationCommand : BaseCommand
	{
		private readonly SerializationHelper _helper;

		public ExportUnicornConfigurationCommand() : this(new SerializationHelper())
		{
			
		}

		public ExportUnicornConfigurationCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override void ProcessRecord()
		{
			var configurations = PipelineConfiguration != null ? new [] { PipelineConfiguration } : UnicornConfigurationManager.Configurations;

			if (!All.IsPresent && PipelineConfiguration == null)
			{
				if(Configurations == null || Configurations.Length == 0) throw new InvalidOperationException("-All was not specified, but neither was -Configurations.");

				var includedConfigs = new HashSet<string>(Configurations, StringComparer.OrdinalIgnoreCase);

				configurations = configurations
					.Where(config => includedConfigs.Contains(config.Name))
					.ToArray();
			}
			
			var console = new PowershellProgressStatus(Host, "Reserialize Unicorn");

			bool success = _helper.ReserializeConfigurations(configurations, console, new WebConsoleLogger(console, MessageType.Debug));

			if (!success || console.HasErrors) throw new InvalidOperationException("Reserialize failed. Review preceding logs for details.");
		}

		[Parameter(ValueFromPipeline = true)]
		public IConfiguration PipelineConfiguration { get; set; }

		[Parameter(Position = 0)]
		public string[] Configurations { get; set; }

		[Parameter]
		public SwitchParameter All { get; set; }
	}
}