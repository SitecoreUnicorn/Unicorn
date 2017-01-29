using System;
using Rainbow.Storage.Sc;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands.Serialization;

namespace Unicorn.UI.Commands
{
	[Serializable]
	public class UnicornDumpTreeCommand : DumpTreeCommand
	{
		private readonly SerializationHelper _helper;

		public UnicornDumpTreeCommand() : this(new SerializationHelper())
		{
			
		}

		public UnicornDumpTreeCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override void Dump(Item item)
		{
			Assert.ArgumentNotNull(item, "item");

			var itemData = new ItemData(item);

			var result = _helper.ReserializeTree(itemData, true);

			if (!result)
			{
				base.Dump(item);
			}
		}
	}
}
