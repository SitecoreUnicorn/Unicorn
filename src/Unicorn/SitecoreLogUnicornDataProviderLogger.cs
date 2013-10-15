using System;
using System.Linq;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn
{
	public class SitecoreLogUnicornDataProviderLogger : IUnicornDataProviderLogger
	{
		public virtual void RenamedItem(ISourceItem sourceItem, string oldName)
		{
			Log.Info("Unicorn: Renamed serialized item to " + sourceItem.Path + " from " + oldName, this);
		}

		public virtual void SavedItem(ISourceItem sourceItem)
		{
			Log.Info("Unicorn: Serialized " + sourceItem.Path + " to disk.", this);
		}

		public virtual void MovedItemToNonIncludedLocation(ISerializedItem existingItem)
		{
			Log.Debug("Unicorn: Moved item " + existingItem.ItemPath + " was moved to a path that was not included in serialization, and the existing serialized item was deleted.", this);
		}

		public virtual void MovedItem(ISourceItem sourceItem, ISourceItem destinationItem)
		{
			Log.Info("Unicorn: Moved serialized item " + sourceItem.Path + " to " + destinationItem.Path, this);
		}

		public virtual void CopiedItem(Func<ISourceItem> sourceItem, ISourceItem copiedItem)
		{
		}

		public virtual void DeletedItem(ISerializedItem existingItem)
		{
			Log.Info("Unicorn: Serialized item " + existingItem.ProviderId + " was deleted due to the source item being deleted.", this);
		}

		public virtual void SaveRejectedAsInconsequential(ItemChanges changes)
		{
			Log.Debug("Unicorn: Ignored save of " + changes.Item.Paths.Path + " because it contained no consequential item changes.", this);
		}
	}
}
