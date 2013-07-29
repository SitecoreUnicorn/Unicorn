using System;
using System.Collections.Generic;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Data;
using System.IO;
using Sitecore;

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

			var changes = Event.ExtractParameter<ItemChanges>(e, 1);

			if (!HasValidChanges(changes)) return;

			base.OnItemSaved(sender, e);
		}

		private bool HasValidChanges(ItemChanges changes)
		{
			foreach (FieldChange change in changes.FieldChanges)
			{
				if(change.OriginalValue == change.Value) continue;
				if (change.FieldID == FieldIDs.Revision) continue;
				if (change.FieldID == FieldIDs.Updated) continue;

				return true;
			}

			Log.Info("Item " + changes.Item.Paths.FullPath + " was saved, but contained no consequential changes so it was not serialized.", this);

			return false;
		}

		/// <summary>
		/// The default serialization event handler does not properly move serialized subitems when the parent is moved (bug 384931)
		/// </summary>
		public new void OnItemMoved(object sender, EventArgs e)
		{
			if (DisabledLocally) return;

			// [0] is the moved item, [1] is the ID of the previous parent item
			var item = Event.ExtractParameter<Item>(e, 0);
			var oldParentId = Event.ExtractParameter<ID>(e, 1);

			if (item == null || oldParentId == (ID)null) return;

			var oldParent = item.Database.GetItem(oldParentId);

			if (oldParent == null) return;
			
			var oldReference = new ItemReference(oldParent).ToString();

			// fix the reference to the old parent to be a reference to the old item path
			oldReference = oldReference + '/' + item.Name;

			var oldSerializationPath = PathUtils.GetDirectoryPath(oldReference);

			FixupDescendants(oldSerializationPath, item);

			if (!Presets.Includes(item))
			{
				// If the preset does not include the destination path, we need to delete the old items from disk
				// https://github.com/kamsar/Unicorn/issues/3
				// Thanks Mike Edwards!
				var oldSerializedItemPath = PathUtils.GetFilePath(oldReference);

				if(File.Exists(oldSerializedItemPath)) File.Delete(oldSerializedItemPath);

				return; // don't invoke the base behavior as we don't need serialization
			}

			// if the source location was not part of the preset, it probably isn't on disk
			// which means base.OnItemMoved will explode quicker than you can say 'bonkers'!
			// so instead we simply dump the new path and children
			// NOTE: it's imperfect because hypothetically children of the new path could be excluded by the preset
			// but whatever, that's a massive edge case.
			if (!Presets.Includes(oldParent))
			{
				Manager.DumpTree(item);
				return;
			}

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

			// the name wasn't actually changed, you sneaky template builder you. Don't write.
			if (oldName.Equals(item.Name, StringComparison.Ordinal)) return;

			if (!Presets.Includes(item)) return;

			// we push this to get updated. Because saving now ignores "inconsquential" changes like a rename that do not change data fields,
			// this keeps renames occurring even if the field changes are inconsequential
			ShadowWriter.PutItem(Operation.Updated, item, item.Parent);

			var reference = new ItemReference(item).ToString();
			var oldReference = reference.Substring(0, reference.LastIndexOf('/') + 1) + oldName;
			
			var oldSerializationPath = PathUtils.GetDirectoryPath(oldReference);

			if(Directory.Exists(oldSerializationPath))
				FixupDescendants(oldSerializationPath, item);
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
					Manager.CleanupPath(parentSerializationPath, false);
			}
		}

		private static void FixupDescendants(string oldSerializationPath, Item modifiedParentItem)
		{
			// if serialized children exist, we want to find children of the *new* item location and
			// re-dump them (this will fix their paths and such)
			if (Directory.Exists(oldSerializationPath))
			{
				// this could get ugly if you moved a giant folder, but that is unlikely unless abusing
				// Unicorn for things it isn't meant for
				var children = modifiedParentItem.Axes.GetDescendants();
				foreach (var child in children)
				{
					if (Presets.Includes(child))
						Manager.DumpItem(child);
				}

				Directory.Delete(oldSerializationPath, true);
			}
		}
	}
}
