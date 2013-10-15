using System;
using System.Linq;
using Sitecore.Data.Items;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn
{
	public interface IUnicornDataProviderLogger
	{
		void RenamedItem(ISourceItem sourceItem, string oldName);

		void SavedItem(ISourceItem sourceItem);

		void MovedItemToNonIncludedLocation(ISerializedItem existingItem);

		void MovedItem(ISourceItem sourceItem, ISourceItem destinationItem);
		void CopiedItem(Func<ISourceItem> sourceItem, ISourceItem copiedItem);

		void DeletedItem(ISerializedItem existingItem);

		void SaveRejectedAsInconsequential(ItemChanges changes);
	}
}
