using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;
using Unicorn.Logging;

namespace Unicorn.Deserialization
{
	public class DefaultDeserializerLogger : IDefaultDeserializerLogger
	{
		private readonly ILogger _logger;
		public DefaultDeserializerLogger(ILogger logger)
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

		public virtual void SkippedMissingTemplateField(Item item, IItemFieldValue field)
		{
			_logger.Warn("* [S] field {0} because it did not exist on template {1}.".FormatWith(field.FieldId, item.TemplateName));
		}

		public virtual void WroteBlobStream(Item item, IItemFieldValue field)
		{

		}

		public virtual void UpdatedChangedFieldValue(Item item, IItemFieldValue field, string oldValue)
		{
			var itemField = item.Fields[new ID(field.FieldId)];
			if (itemField.Shared)
				_logger.Debug("* [U] {0}".FormatWith(itemField.Name));
			else
				_logger.Debug("* [U] {0}#{1}: {2}".FormatWith(item.Language.Name, item.Version.Number, itemField.Name));
		}

		public virtual void ResetFieldThatDidNotExistInSerialized(Field field)
		{

		}

		public void SkippedPastingIgnoredField(Item item, IItemFieldValue field)
		{
			
		}
	}
}