using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets;
using Kamsar.WebConsole;
using Rainbow.Model;
using Sitecore.Data.Items;
using Sitecore.Pipelines;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncEnd;
using ItemData = Rainbow.Storage.Sc.ItemData;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// # PARTIAL SYNCING
	/// Get-Item "/sitecore/content" | Sync-UnicornItem # Sync a single item (note: must be under Unicorn control)
	/// Get-ChildItem "/sitecore/content" | Sync-UnicornItem # Sync multiple items (note: all must be under Unicorn control)
	/// Get-Item "/sitecore/content" | Sync-UnicornItem -Recurse # Sync an entire item tree (note: must be under Unicorn control)
	/// </summary>
	[Cmdlet("Sync", "UnicornItem")]
	public class SyncUnicornItemCommand : BaseCommand
	{
		private readonly SerializationHelper _helper;

		public SyncUnicornItemCommand() : this(new SerializationHelper())
		{
			
		}

		public SyncUnicornItemCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override void ProcessRecord()
		{
			var touchedConfigs = new List<IConfiguration>();

				IItemData itemData = new ItemData(Item);
				var configuration = _helper.GetConfigurationsForItem(itemData).FirstOrDefault(); // if multiple configs contain item, load from first one

				if (configuration == null) throw new InvalidOperationException($"{itemData.GetDisplayIdentifier()} was not part of any Unicorn configurations.");

				touchedConfigs.Add(configuration);

				var logger = new WebConsoleLogger(new PowershellProgressStatus(Host, "Partial Sync Unicorn"), LogLevel);

				var helper = configuration.Resolve<SerializationHelper>();
				var targetDataStore = configuration.Resolve<ITargetDataStore>();

				itemData = targetDataStore.GetByPathAndId(itemData.Path, itemData.Id, itemData.DatabaseName);

				if (itemData == null)
				{
					throw new InvalidOperationException($"Could not do partial sync of {Item.Database.Name}:{Item.Paths.FullPath} because it was not serialized. You may need to perform initial serialization.");
				}

			try
			{
				logger.Info(
					$"Processing partial Unicorn configuration {itemData.GetDisplayIdentifier()} (Config: {configuration.Name})");

				using (new LoggingContext(logger, configuration))
				{
					if (Recurse.IsPresent)
					{
						helper.SyncTree(configuration, partialSyncRoot: itemData);
					}
					else
					{
						var sourceStore = configuration.Resolve<ISourceDataStore>();
						sourceStore.Save(itemData);
					}
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex);
				throw;
			}

			CorePipeline.Run("unicornSyncEnd", new UnicornSyncEndPipelineArgs(new SitecoreLogger(), true, touchedConfigs.ToArray()));
		}

		[Parameter(ValueFromPipeline = true, Mandatory = true)]
		public Item Item { get; set; }

		[Parameter]
		public SwitchParameter Recurse { get; set; }

		[Parameter]
		public MessageType LogLevel { get; set; } = MessageType.Debug;
	}
}