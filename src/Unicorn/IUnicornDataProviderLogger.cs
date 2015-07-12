using System;
using Rainbow.Model;
using Sitecore.Data.Items;

namespace Unicorn
{
	/// <summary>
	/// Provides log messages that can be responded to from the UnicornDataProvider.
	/// </summary>
	public interface IUnicornDataProviderLogger
	{
		void RenamedItem(string providerName, IItemData sourceItemData, string oldName);

		void SavedItem(string providerName, IItemData sourceItemData, string triggerReason);

		void MovedItemToNonIncludedLocation(string providerName, IItemData existingItemData);

		void MovedItem(string providerName, IItemData sourceItemData, IItemData destinationItemData);
		void CopiedItem(string providerName, Func<IItemData> sourceItem, IItemData copiedItemData);

		void DeletedItem(string providerName, IItemData existingItemData);

		void SaveRejectedAsInconsequential(string providerName, ItemChanges changes);
	}
}
