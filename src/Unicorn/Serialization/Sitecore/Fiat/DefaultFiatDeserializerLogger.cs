using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.StringExtensions;
using Unicorn.Logging;

namespace Unicorn.Serialization.Sitecore.Fiat
{
	public class DefaultFiatDeserializerLogger : IFiatDeserializerLogger
	{
		private readonly ILogger _logger;

		public DefaultFiatDeserializerLogger(ILogger logger)
		{
			_logger = logger;
		}

		public virtual void CreatedNewItem(Item targetItem)
		{
			
		}

		public virtual void MovedItemToNewParent(Item newParentItem, Item oldParentItem, Item movedItem)
		{
			_logger.Debug("* [M] from {0} to {1}".FormatWith(oldParentItem.ID, newParentItem.ID));
		}

		public virtual void RemovingOrphanedVersion(Item versionToRemove)
		{
			_logger.Debug("* [D] {0}#{1}".FormatWith(versionToRemove.Language.Name, versionToRemove.Version.Number));
		}

		public virtual void RenamedItem(Item targetItem, string oldName)
		{
			_logger.Debug("* [R] from {0} to {1}".FormatWith(oldName, targetItem.Name));
		}

		public virtual void ChangedBranchTemplate(Item targetItem, string oldBranchId)
		{
			
		}

		public virtual void ChangedTemplate(Item targetItem, TemplateItem oldTemplate)
		{
			_logger.Debug("* [T] from {0} to {1}".FormatWith(oldTemplate.Name, targetItem.TemplateName));
		}

		public virtual void AddedNewVersion(Item newVersion)
		{
			_logger.Debug("* [A] version {0}#{1}".FormatWith(newVersion.Language.Name, newVersion.Version.Number));
		}

		public virtual void SkippedMissingTemplateField(Item item, SyncField field)
		{
			_logger.Warn("* Skipped field {0} because it did not exist on template {1}.".FormatWith(field.FieldName, item.TemplateName));
		}

		public virtual void WroteBlobStream(Item item, SyncField field)
		{
			
		}

		public virtual void UpdatedChangedFieldValue(Item item, SyncField field, string oldValue)
		{
			if(item.Fields[field.FieldID].Shared)
				_logger.Debug("* [U] {0}".FormatWith(field.FieldName));
			else
				_logger.Debug("* [U] {0}#{1}: {2}".FormatWith(item.Language.Name, item.Version.Number, field.FieldName));
		}


		public void ResetFieldThatDidNotExistInSerialized(Field field)
		{
			
		}
	}
}