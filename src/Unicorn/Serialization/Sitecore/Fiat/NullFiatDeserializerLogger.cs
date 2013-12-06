using Sitecore.Data.Items;
using Sitecore.Data.Serialization.ObjectModel;

namespace Unicorn.Serialization.Sitecore.Fiat
{
	public class NullFiatDeserializerLogger : IFiatDeserializerLogger
	{
		public void CreatedNewItem(Item targetItem)
		{
			
		}

		public void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem)
		{
			
		}

		public void RemovingOrphanedVersion(Item versionToRemove)
		{
			
		}

		public void RenamedItem(Item targetItem, string oldName)
		{
			
		}

		public void ChangedBranchTemplate(Item targetItem, string oldBranchId)
		{
			
		}

		public void ChangedTemplate(Item targetItem, TemplateItem oldTemplate)
		{
			
		}

		public void AddedNewVersion(Item newVersion)
		{
			
		}

		public void SkippedMissingTemplateField(Item item, SyncField field)
		{
			
		}

		public void WroteBlobStream(Item item, SyncField field)
		{
			
		}

		public void UpdatedChangedFieldValue(Item item, SyncField field, string oldValue)
		{
			
		}
	}
}