using Sitecore.Data.Items;
using Sitecore.Data.Serialization.ObjectModel;

namespace Unicorn.Serialization.Sitecore.Fiat
{
	public interface IFiatDeserializerLogger
	{
		void CreatedNewItem(Item targetItem);

		void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem);

		void RemovingOrphanedVersion(Item versionToRemove);

		void RenamedItem(Item targetItem, string oldName);

		void ChangedBranchTemplate(Item targetItem, string oldBranchId);

		void ChangedTemplate(Item targetItem, TemplateItem oldTemplate);

		void AddedNewVersion(Item newVersion);

		void SkippedMissingTemplateField(Item item, SyncField field);

		void WroteBlobStream(Item item, SyncField field);

		void UpdatedChangedFieldValue(Item item, SyncField field, string oldValue);
	}
}
