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

			if (item == null) return;

			if (!Presets.Includes(item)) return;

			base.OnItemSaved(sender, e);
		}

		public new void OnItemMoved(object sender, EventArgs e)
		{
			var item = Event.ExtractParameter<Item>(e, 0);

			if (item == null) return;

			if (!Presets.Includes(item)) return;
		
			base.OnItemMoved(sender, e);
		}

		/// <summary>
		/// The default serialization event handler does not catch duplications (bug 384823)
		/// </summary>
		public void OnItemCopied(object sender, EventArgs e)
		{
			if (DisabledLocally) return;

			// param 0 = source item, param 1 = destination item
			var item = Event.ExtractParameter<Item>(e, 1);

			if (item == null) return;

			if (!Presets.Includes(item)) return;	
		
			ShadowWriter.PutItem(Operation.Updated, item, item.Parent);

			// putting the root isn't enough; if it has children those also need to get serialized
			var descendants = item.Axes.GetDescendants();

			foreach(var descendant in descendants)
				ShadowWriter.PutItem(Operation.Updated, descendant, descendant.Parent);
		}

		/// <summary>
		/// The default serialization event handler actually deletes serialized subitems if the parent item is renamed. This patches that behavior to preserve subitem files. (bug 384931)
		/// </summary>
		public void OnItemRenamed(object sender, EventArgs e)
		{
			if (DisabledLocally) return;

			// param 0 = renamed item, param 1 = old item name
			var item = Event.ExtractParameter<Item>(e, 0);
			var oldName = Event.ExtractParameter<string>(e, 1);

			if (item == null || oldName == null) return;

			if (!Presets.Includes(item)) return;

			var reference = new ItemReference(item).ToString();
			var oldReference = reference.Substring(0, reference.LastIndexOf('/') + 1) + oldName;
			
			var oldSerializationPath = PathUtils.GetDirectoryPath(oldReference);
			var newSerializationPath = PathUtils.GetDirectoryPath(reference);

			if(Directory.Exists(oldSerializationPath) && !Directory.Exists(newSerializationPath))
				Directory.Move(oldSerializationPath, newSerializationPath);
		}

		public new void OnItemVersionRemoved(object sender, EventArgs e)
		{
			var item = Event.ExtractParameter<Item>(e, 0);

			if (item == null) return;

			if (!Presets.Includes(item)) return;

			base.OnItemVersionRemoved(sender, e);
		}

		// NOTE on DELETION
		// Because the item:deleted event does not have a full path assigned to it, we cannot filter deletes other than by their parent item path if that exists
		// This shouldn't be a problem in general because includes and excludes are recursive unless I haven't thought of something.
		//
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

				if(Directory.Exists(parentSerializationPath) && Presets.Includes(parentItem))
					base.OnItemDeleted(sender, e);
			}
		}
	}
}
