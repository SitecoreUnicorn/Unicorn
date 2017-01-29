using System;
using Rainbow.Storage.Sc;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands.Serialization;

namespace Unicorn.UI.Commands
{
	[Serializable]
	public class UnicornDumpItemCommand : DumpItemCommand
	{
		private readonly SerializationHelper _helper;

		public UnicornDumpItemCommand() : this(new SerializationHelper())
		{
			
		}

		public UnicornDumpItemCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override void Dump(Item item)
		{
			Assert.ArgumentNotNull(item, "item");

			var itemData = new ItemData(item);

			var result = _helper.ReserializeItem(itemData);

			if (!result)
			{
				base.Dump(item);
			}
		}
	}
}
