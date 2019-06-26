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
	/// # SYNCING
	/// Sync-UnicornConfiguration "Foundation.Foo" # Sync one
	/// Sync-UnicornConfiguration @("Foundation.Foo", "Foundation.Bar") # Sync multiple by name
	/// Get-UnicornConfiguration "Foundation.*" | Sync-UnicornConfiguration # Sync multiple from pipeline
	/// Get-UnicornConfiguration | Sync-UnicornConfiguration -SkipTransparent # Sync all, except transparent sync
	/// Sync-UnicornConfiguration -LogLevel Warn # Optionally set log output level (Debug, Info, Warn, Error)
	/// </summary>
	[Cmdlet("Sync", "UnicornConfiguration")]
	public class SyncUnicornConfigurationCommand : BaseCommand
	{
		private readonly SerializationHelper _helper;
		private List<IConfiguration> _configurations = new List<IConfiguration>();

		public SyncUnicornConfigurationCommand() : this(new SerializationHelper())
		{

		}

		public SyncUnicornConfigurationCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override void ProcessRecord()
		{
			if (PipelineConfiguration != null)
			{
				_configurations.Add(PipelineConfiguration);
				return;
			}

			if (Configuration == null) throw new InvalidOperationException("-Configurations not specified, and no pipeline input.");

			var config = UnicornConfigurationManager.Configurations
				.FirstOrDefault(cfg => cfg.Name.Equals(Configuration, StringComparison.OrdinalIgnoreCase));

			if (config != null) _configurations.Add(config);
		}

		protected override void EndProcessing()
		{
			var configurations = _configurations.ToArray();

			if (SkipTransparent.IsPresent) configurations = configurations.SkipTransparentSync().ToArray();

			var console = new PowershellProgressStatus(Host, "Sync Unicorn");

			bool success = _helper.SyncConfigurations(configurations, console, new WebConsoleLogger(console, LogLevel.ToString()));

			if (!success || console.HasErrors) throw new InvalidOperationException("Sync failed. Review preceding logs for details.");
		}

		[Parameter(ValueFromPipeline = true)]
		public IConfiguration PipelineConfiguration { get; set; }

		[Parameter(Position = 0)]
		public string Configuration { get; set; }

		[Parameter]
		public SwitchParameter SkipTransparent { get; set; }

		[Parameter]
		public MessageType LogLevel { get; set; } = MessageType.Debug;
	}
}