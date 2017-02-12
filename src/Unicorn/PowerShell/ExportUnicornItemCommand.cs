using System;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets;
using Rainbow.Model;
using Sitecore.Data.Items;
using Unicorn.Data;
using ItemData = Rainbow.Storage.Sc.ItemData;

namespace Unicorn.PowerShell
{
	/// <summary>
	/// # PARTIAL RESERIALIZATION
	/// Get-Item "/sitecore/content" | Export-UnicornItem # Reserialize a single item (note: must be under Unicorn control)
	/// Get-ChildItem "/sitecore/content" | Export-UnicornItem # Reserialize multiple items (note: all must be under Unicorn control)
	/// Get-Item "/sitecore/content" | Export-UnicornItem -Recurse # Reserialize an entire item tree (note: must be under Unicorn control)
	/// </summary>
	[Cmdlet("Export", "UnicornItem")]
	public class ExportUnicornItemCommand : BaseCommand
	{
		private readonly SerializationHelper _helper;

		public ExportUnicornItemCommand() : this(new SerializationHelper())
		{

		}

		public ExportUnicornItemCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override void ProcessRecord()
		{
			IItemData itemData = new ItemData(Item);

			if (Recurse.IsPresent)
			{
				if (!_helper.ReserializeTree(itemData)) throw new InvalidOperationException($"{itemData.GetDisplayIdentifier()} was not part of any Unicorn configuration.");
			}
			else
			{
				if (!_helper.ReserializeItem(itemData)) throw new InvalidOperationException($"{itemData.GetDisplayIdentifier()} was not part of any Unicorn configuration.");
			}
		}

		[Parameter(ValueFromPipeline = true, Mandatory = true)]
		public Item Item { get; set; }

		[Parameter]
		public SwitchParameter Recurse { get; set; }
	}
}