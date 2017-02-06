using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using Cognifide.PowerShell.Commandlets;
using Kamsar.WebConsole;
using Sitecore.Data.Items;
using Unicorn.Configuration;
using Unicorn.Logging;

namespace Unicorn.PowerShell
{
	[OutputType(typeof(bool)), Cmdlet("Sync", "UnicornConfiguration")]
	public class SyncUnicornConfigurationCommand : BaseCommand
	{
		private readonly SerializationHelper _helper;

		public SyncUnicornConfigurationCommand() : this(new SerializationHelper())
		{
			
		}

		public SyncUnicornConfigurationCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override void ProcessRecord()
		{
			var configurations = UnicornConfigurationManager.Configurations;

			if (!All.IsPresent)
			{
				if(Configurations == null || Configurations.Length == 0) throw new InvalidOperationException("-All was not specified, but neither was -Configurations.");

				var includedConfigs = new HashSet<string>(Configurations, StringComparer.OrdinalIgnoreCase);

				configurations = configurations
					.Where(config => includedConfigs.Contains(config.Name))
					.ToArray();
			}

			if (SkipTransparent.IsPresent) configurations = configurations.SkipTransparentSync().ToArray();

			var console = new PowershellProgressStatus(Host, "Sync Unicorn");

			bool success = _helper.SyncConfigurations(configurations, console, new WebConsoleLogger(console, LogLevel.ToString()));

			if (!success) throw new InvalidOperationException("Sync failed. Review preceding logs for details.");

			WriteObject(success);
		}

		[Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
		public string[] Configurations { get; set; }

		[Parameter]
		public SwitchParameter All { get; set; }

		[Parameter]
		public SwitchParameter SkipTransparent { get; set; }

		[Parameter]
		public MessageType LogLevel { get; set; } = MessageType.Info;
	}
}