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
		public virtual void RenamedItem(string providerName, ISourceItem sourceItem, string oldName)
		{
			Log.Info(string.Format("{0}: Renamed serialized item to {1} from {2}", providerName, sourceItem.Path, oldName), this);
		}

		public virtual void SavedItem(string providerName, ISourceItem sourceItem)
		{
			Log.Info(string.Format("{0}: Serialized {1} ({2}) to disk.", providerName, sourceItem.Path, sourceItem.Id), this);
		}

		public virtual void MovedItemToNonIncludedLocation(string providerName, ISerializedItem existingItem)
		{
			Log.Debug(string.Format("{0}: Moved item {1} was moved to a path that was not included in serialization, and the existing serialized item was deleted.", providerName, existingItem.ItemPath), this);
		}

		public virtual void MovedItem(string providerName, ISourceItem sourceItem, ISourceItem destinationItem)
		{
			Log.Info(string.Format("{0}: Moved serialized item {1} ({2}) to {3}", providerName, sourceItem.Path, sourceItem.Id, destinationItem.Path), this);
		}

		public virtual void CopiedItem(string providerName, Func<ISourceItem> sourceItem, ISourceItem copiedItem)
		{
		}

		public virtual void DeletedItem(string providerName, ISerializedItem existingItem)
		{
			Log.Info(string.Format("{0}: Serialized item {1} was deleted due to the source item being deleted.", providerName, existingItem.ProviderId), this);
		}

		public virtual void SaveRejectedAsInconsequential(string providerName, ItemChanges changes)
		{
			Log.Debug(string.Format("{0}: Ignored save of {1} because it contained no consequential item changes.", providerName, changes.Item.Paths.Path), this);
		}
	}
}
