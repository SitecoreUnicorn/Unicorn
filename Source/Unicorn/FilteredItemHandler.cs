using System;
using System.Collections.Generic;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Events;
using Sitecore.Data;
using System.IO;

namespace Unicorn
{
	/// <summary>
	/// This class extends the default serialization item handler to allow it to process preset paths only
	/// like you can configure for the serialization page. This is great if you only want to serialize part of your database.
	/// 
	/// See the serialization guide for details on creating presets.
	/// </summary>
	public class FilteredItemHandler : ItemHandler
	{
		private static readonly IList<IncludeEntry> Presets = new List<IncludeEntry>(); 

		static FilteredItemHandler()
		{
			Presets = SerializationUtility.GetPreset();
		}

		public new void OnItemSaved(object sender, EventArgs e)
		{
			var item = Event.ExtractParameter<Item>(e, 0);

			if (item != null)
			{
				if (Presets.Includes(item))
					base.OnItemSaved(sender, e);
			}
		}

		public new void OnItemMoved(object sender, EventArgs e)
		{
			var item = Event.ExtractParameter<Item>(e, 0);

			if (item != null)
			{
				if (Presets.Includes(item))
					base.OnItemMoved(sender, e);
			}
		}

		public new void OnItemVersionRemoved(object sender, EventArgs e)
		{
			var item = Event.ExtractParameter<Item>(e, 0);

			if (item != null)
			{
				if (Presets.Includes(item))
					base.OnItemVersionRemoved(sender, e);
			}
		}

		// NOTE on DELETION
		// Because the item:deleted event does not have a full path assigned to it, we cannot filter deletes
		// So, all deleted items will cause associated serialization deletes just like the stock handler.
		// Most of the time this shouldn't be a problem at all since if you remove something that isn't already serialized this does nothing,
		// and if it is serialized you probably want it dead anyway since it's gone from Sitecore.
		// NOTE: this method works around a bug in Sitecore (380479) that causes it to kill the app pool if a delete is sent for an item without an
		// existing parent serialization folder. It verifies the parent directory exists before passing it to the base method.
		public new void OnItemDeleted(object sender, EventArgs e)
		{
			var item = Event.ExtractParameter<Item>(e, 0);
			var parentId = Event.ExtractParameter<ID>(e, 1);

			if (item != null && parentId != (ID)null)
			{
				var parentItem = item.Database.GetItem(parentId);
				if (parentItem == null) return;

				var parentSerializationPath = PathUtils.GetDirectoryPath(new ItemReference(parentItem).ToString());

				if(Directory.Exists(parentSerializationPath))
					base.OnItemDeleted(sender, e);
			}
		}
	}
}
