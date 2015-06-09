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
		void RenamedItem(string providerName, ISerializableItem sourceItem, string oldName);

		void SavedItem(string providerName, ISerializableItem sourceItem, string triggerReason);

		void MovedItemToNonIncludedLocation(string providerName, ISerializableItem existingItem);

		void MovedItem(string providerName, ISerializableItem sourceItem, ISerializableItem destinationItem);
		void CopiedItem(string providerName, Func<ISerializableItem> sourceItem, ISerializableItem copiedItem);

		void DeletedItem(string providerName, ISerializableItem existingItem);

		void SaveRejectedAsInconsequential(string providerName, ItemChanges changes);
	}
}
