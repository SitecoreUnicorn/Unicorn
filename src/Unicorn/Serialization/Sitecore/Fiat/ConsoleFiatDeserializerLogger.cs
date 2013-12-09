using Kamsar.WebConsole;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization.ObjectModel;

namespace Unicorn.Serialization.Sitecore.Fiat
{
	public class ConsoleFiatDeserializerLogger : IFiatDeserializerLogger
	{
		private readonly IProgressStatus _progress;

		public ConsoleFiatDeserializerLogger(IProgressStatus progress)
		{
			_progress = progress;
		}

		public virtual void CreatedNewItem(Item targetItem)
		{
			
		}

		public virtual void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem)
		{
			_progress.ReportStatus("- [M] from {0} to {1}", MessageType.Debug, oldParentItem.ID, newParentItem.ID);
		}

		public virtual void RemovingOrphanedVersion(Item versionToRemove)
		{
			_progress.ReportStatus("- [D] {0}#{1}", MessageType.Debug, versionToRemove.Language.Name, versionToRemove.Version.Number);
		}

		public virtual void RenamedItem(Item targetItem, string oldName)
		{
			_progress.ReportStatus("- [R] from {0} to {1}", MessageType.Debug, oldName, targetItem.Name);
		}

		public virtual void ChangedBranchTemplate(Item targetItem, string oldBranchId)
		{
			
		}

		public virtual void ChangedTemplate(Item targetItem, TemplateItem oldTemplate)
		{
			_progress.ReportStatus("- [T] from {0} to {1}", MessageType.Debug, oldTemplate.Name, targetItem.TemplateName);
		}

		public virtual void AddedNewVersion(Item newVersion)
		{
			_progress.ReportStatus("- [A] version {0}#{1}", MessageType.Debug, newVersion.Language.Name, newVersion.Version.Number);
		}

		public virtual void SkippedMissingTemplateField(Item item, SyncField field)
		{
			_progress.ReportStatus("- Skipped field {0} because it did not exist on template {1}.", MessageType.Warning, field.FieldName, item.TemplateName);
		}

		public virtual void WroteBlobStream(Item item, SyncField field)
		{
			
		}

		public virtual void UpdatedChangedFieldValue(Item item, SyncField field, string oldValue)
		{
			if(item.Fields[field.FieldID].Shared)
				_progress.ReportStatus(" - [U] {0}", MessageType.Debug, field.FieldName);
			else
				_progress.ReportStatus("- [U] {0}#{1}: {2}", MessageType.Debug, item.Language.Name, item.Version.Number, field.FieldName);
		}


		public void ResetFieldThatDidNotExistInSerialized(Field field)
		{
			
		}
	}
}