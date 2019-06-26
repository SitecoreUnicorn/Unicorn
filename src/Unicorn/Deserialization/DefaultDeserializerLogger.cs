using System.Diagnostics.CodeAnalysis;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Unicorn.Logging;

namespace Unicorn.Deserialization
{
	[ExcludeFromCodeCoverage]
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
			_logger.Debug($"* [M] from {oldParentItem.ID} to {newParentItem.ID}");
		}

		public virtual void RemovingOrphanedVersion(Item versionToRemove)
		{
			_logger.Debug($"* [D] {versionToRemove.Language.Name}#{versionToRemove.Version.Number}");
		}

		public virtual void RenamedItem(Item targetItem, string oldName)
		{
			_logger.Debug($"* [R] from '{oldName}' to '{targetItem.Name}'");
		}

		public virtual void ChangedBranchTemplate(Item targetItem, string oldBranchId)
		{
			_logger.Debug($"* [B] from {oldBranchId} to {targetItem.BranchId}");
		}

		public virtual void ChangedTemplate(Item targetItem, TemplateItem oldTemplate)
		{
			if(oldTemplate != null)
				_logger.Debug($"* [T] from {oldTemplate.Name} to {targetItem.TemplateName}");
			else
				_logger.Debug($"* [T] to {targetItem.TemplateName}");
		}

		public virtual void AddedNewVersion(Item newVersion)
		{
			_logger.Debug($"* [A] version {newVersion.Language.Name}#{newVersion.Version.Number}");
		}

		public virtual void WroteBlobStream(Item item, IItemFieldValue field)
		{

		}

		public virtual void UpdatedChangedFieldValue(Item item, IItemFieldValue field, string oldValue)
		{
			var itemField = item.Fields[new ID(field.FieldId)];
			if (itemField.Shared)
				_logger.Debug($"* [U] {itemField.Name}");
			else if(itemField.Unversioned)
				_logger.Debug($"* [U] {item.Language.Name}: {itemField.Name}");
			else
				_logger.Debug($"* [U] {item.Language.Name}#{item.Version.Number}: {itemField.Name}");
		}

		public virtual void ResetFieldThatDidNotExistInSerialized(Field field)
		{

		}

		public void SkippedPastingIgnoredField(Item item, IItemFieldValue field)
		{
			
		}
	}
}