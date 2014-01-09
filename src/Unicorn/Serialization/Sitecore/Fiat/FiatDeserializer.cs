using System;
using System.Collections;
using System.IO;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Unicorn.Dependencies;

namespace Unicorn.Serialization.Sitecore.Fiat
{
	public class FiatDeserializer
	{
		private readonly IFiatDeserializerLogger _logger;

		public FiatDeserializer(IFiatDeserializerLogger logger = null)
		{
			logger = logger ?? Registry.Current.Resolve<IFiatDeserializerLogger>();

			Assert.ArgumentNotNull(logger, "logger");

			_logger = logger;
		}

		/// <summary>
		/// Pastes SyncItem into the database.
		/// 
		/// </summary>
		/// <param name="syncItem">The sync item.</param>
		/// <param name="ignoreMissingTemplateFields">Whether to ignore fields in the serialized item that do not exist on the Sitecore template</param>
		/// <returns>
		/// The pasted item.
		/// </returns>
		/// <exception cref="T:Sitecore.Data.Serialization.Exceptions.ParentItemNotFoundException"><c>ParentItemNotFoundException</c>.</exception><exception cref="T:System.Exception"><c>Exception</c>.</exception><exception cref="T:Sitecore.Data.Serialization.Exceptions.ParentForMovedItemNotFoundException"><c>ParentForMovedItemNotFoundException</c>.</exception>
		public Item PasteSyncItem(SyncItem syncItem, bool ignoreMissingTemplateFields)
		{
			if (syncItem == null)
				return null;

			Database database = Factory.GetDatabase(syncItem.DatabaseName);

			Item destinationParentItem = database.GetItem(syncItem.ParentID);
			ID itemId = ID.Parse(syncItem.ID);
			Item targetItem = database.GetItem(itemId);
			bool newItemWasCreated = false;

			// the target item did not yet exist, so we need to start by creating it
			if (targetItem == null)
			{
				targetItem = CreateTargetItem(syncItem, destinationParentItem);

				_logger.CreatedNewItem(targetItem);

				newItemWasCreated = true;
			}
			else
			{
				// check if the parent of the serialized item does not exist
				// which, since the target item is known to exist, means that
				// the serialized item was moved but its new parent item does
				// not exist to paste it under
				if (destinationParentItem == null)
				{
					throw new ParentForMovedItemNotFoundException
					{
						ParentID = syncItem.ParentID,
						Item = targetItem
					};
				}

				// if the parent IDs mismatch that means we need to move the existing
				// target item to its new parent from the serialized item
				if (destinationParentItem.ID != targetItem.ParentID)
				{
					var oldParent = targetItem.Parent;
					targetItem.MoveTo(destinationParentItem);
					_logger.MovedItemToNewParent(destinationParentItem, oldParent, targetItem);
				}
			}
			try
			{
				ChangeTemplateIfNeeded(syncItem, targetItem);
				RenameIfNeeded(syncItem, targetItem);
				ResetTemplateEngineIfItemIsTemplate(targetItem);

				using (new EditContext(targetItem))
				{
					targetItem.RuntimeSettings.ReadOnlyStatistics = true;
					targetItem.RuntimeSettings.SaveAll = true;

					foreach (Field field in targetItem.Fields)
					{
						if (field.Shared && syncItem.SharedFields.All(x => x.FieldID != field.ID.ToString()))
						{
							_logger.ResetFieldThatDidNotExistInSerialized(field);
							field.Reset();
						}
					}

					foreach (SyncField field in syncItem.SharedFields)
						PasteSyncField(targetItem, field, ignoreMissingTemplateFields);
				}

				ClearCaches(database, itemId);
				targetItem.Reload();
				ResetTemplateEngineIfItemIsTemplate(targetItem);

				Hashtable versionTable = CommonUtils.CreateCIHashtable();

				// this version table allows us to detect and remove orphaned versions that are not in the
				// serialized version, but are in the database version
				foreach (Item version in targetItem.Versions.GetVersions(true))
					versionTable[version.Uri] = null;

				foreach (SyncVersion syncVersion in syncItem.Versions)
				{
					var version = PasteSyncVersion(targetItem, syncVersion, ignoreMissingTemplateFields);
					if (versionTable.ContainsKey(version.Uri))
						versionTable.Remove(version.Uri);
				}

				foreach (ItemUri uri in versionTable.Keys)
				{
					var versionToRemove = Database.GetItem(uri);

					_logger.RemovingOrphanedVersion(versionToRemove);

					versionToRemove.Versions.RemoveVersion();
				}

				ClearCaches(targetItem.Database, targetItem.ID);

				return targetItem;
			}
			catch (ParentForMovedItemNotFoundException)
			{
				throw;
			}
			catch (ParentItemNotFoundException)
			{
				throw;
			}
			catch (FieldIsMissingFromTemplateException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (newItemWasCreated)
				{
					targetItem.Delete();
					ClearCaches(database, itemId);
				}
				throw new Exception("Failed to paste item: " + syncItem.ItemPath, ex);
			}
		}

		protected void RenameIfNeeded(SyncItem syncItem, Item targetItem)
		{
			if (targetItem.Name != syncItem.Name || targetItem.BranchId.ToString() != syncItem.BranchId)
			{
				string oldName = targetItem.Name;
				string oldBranchId = targetItem.BranchId.ToString();

				using (new EditContext(targetItem))
				{
					targetItem.RuntimeSettings.ReadOnlyStatistics = true;
					targetItem.Name = syncItem.Name;
					targetItem.BranchId = ID.Parse(syncItem.BranchId);
				}

				ClearCaches(targetItem.Database, targetItem.ID);
				targetItem.Reload();

				if (oldName != syncItem.Name)
					_logger.RenamedItem(targetItem, oldName);

				if (oldBranchId != syncItem.BranchId)
					_logger.ChangedBranchTemplate(targetItem, oldBranchId);
			}
		}

		protected void ChangeTemplateIfNeeded(SyncItem syncItem, Item targetItem)
		{
			if (targetItem.TemplateID.ToString() != syncItem.TemplateID)
			{
				var oldTemplate = targetItem.Template;
				var newTemplate = targetItem.Database.Templates[ID.Parse(syncItem.TemplateID)];

				Assert.IsNotNull(newTemplate, "Cannot change template of {0} because its new template {1} does not exist!", targetItem.ID, syncItem.TemplateID);

				using (new EditContext(targetItem))
				{
					targetItem.RuntimeSettings.ReadOnlyStatistics = true;
					targetItem.ChangeTemplate(newTemplate);
				}

				_logger.ChangedTemplate(targetItem, oldTemplate);

				ClearCaches(targetItem.Database, targetItem.ID);
				targetItem.Reload();
			}
		}

		protected Item CreateTargetItem(SyncItem syncItem, Item destinationParentItem)
		{
			Database database = Factory.GetDatabase(syncItem.DatabaseName);
			if (destinationParentItem == null)
			{
				throw new ParentItemNotFoundException
				{
					ParentID = syncItem.ParentID,
					ItemID = syncItem.ID
				};
			}

			var templateId = ID.Parse(syncItem.TemplateID);
			var itemId = ID.Parse(syncItem.ID);

			AssertTemplate(database, templateId);

			Item targetItem = ItemManager.AddFromTemplate(syncItem.Name, templateId, destinationParentItem, itemId);
			targetItem.Versions.RemoveAll(true);

			return targetItem;
		}

		/// <summary>
		/// Pastes single version from ItemDom into the item
		/// 
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="syncVersion">The sync version.</param>
		/// <param name="ignoreMissingTemplateFields">Whether to ignore fields in the serialized item that do not exist on the Sitecore template</param>
		/// <returns>The version that was pasted</returns>
		protected virtual Item PasteSyncVersion(Item item, SyncVersion syncVersion, bool ignoreMissingTemplateFields)
		{
			Language language = Language.Parse(syncVersion.Language);
			var targetVersion = global::Sitecore.Data.Version.Parse(syncVersion.Version);
			Item languageItem = item.Database.GetItem(item.ID, language);
			Item languageVersionItem = languageItem.Versions[targetVersion];

			if (languageVersionItem == null)
			{
				languageVersionItem = languageItem.Versions.AddVersion();
				_logger.AddedNewVersion(languageVersionItem);
			}

// ReSharper disable once SimplifyLinqExpression
			if (!languageVersionItem.Versions.GetVersionNumbers().Any(x => x.Number == languageVersionItem.Version.Number))
			{
				_logger.AddedNewVersion(languageVersionItem);
			}

			using (new EditContext(languageVersionItem))
			{
				languageVersionItem.RuntimeSettings.ReadOnlyStatistics = true;

				if (languageVersionItem.Versions.Count == 0)
					languageVersionItem.Fields.ReadAll();

				foreach (Field field in languageVersionItem.Fields)
				{
					if (!field.Shared && syncVersion.Fields.All(x => x.FieldID != field.ID.ToString()))
					{
						_logger.ResetFieldThatDidNotExistInSerialized(field);
						field.Reset();
					}
				}

				bool wasOwnerFieldParsed = false;
				foreach (SyncField field in syncVersion.Fields)
				{
					ID result;
					if (ID.TryParse(field.FieldID, out result) && result == FieldIDs.Owner)
						wasOwnerFieldParsed = true;

					PasteSyncField(languageVersionItem, field, ignoreMissingTemplateFields);
				}

				if (!wasOwnerFieldParsed)
					languageVersionItem.Fields[FieldIDs.Owner].Reset();
			}

			ClearCaches(languageVersionItem.Database, languageVersionItem.ID);
			ResetTemplateEngineIfItemIsTemplate(languageVersionItem);

			return languageVersionItem;
		}

		/// <summary>
		/// Inserts field value into item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="field">The field.</param>
		/// <param name="ignoreMissingTemplateFields">Whether to ignore fields in the serialized item that do not exist on the Sitecore template</param>
		/// <exception cref="T:Sitecore.Data.Serialization.Exceptions.FieldIsMissingFromTemplateException"/>
		protected virtual void PasteSyncField(Item item, SyncField field, bool ignoreMissingTemplateFields)
		{
			Template template = AssertTemplate(item.Database, item.TemplateID);
			if (template.GetField(field.FieldID) == null)
			{
				item.Database.Engines.TemplateEngine.Reset();
				template = AssertTemplate(item.Database, item.TemplateID);
			}
			if (template.GetField(field.FieldID) == null)
			{
				if (!ignoreMissingTemplateFields)
					throw new FieldIsMissingFromTemplateException("Field '" + field.FieldName + "' does not exist in template '" + template.Name + "'", FileUtil.MakePath(item.Template.InnerItem.Database.Name, item.Template.InnerItem.Paths.FullPath), FileUtil.MakePath(item.Database.Name, item.Paths.FullPath), item.ID);

				_logger.SkippedMissingTemplateField(item, field);
				return;
			}

			Field itemField = item.Fields[ID.Parse(field.FieldID)];
			if (itemField.IsBlobField && !ID.IsID(field.FieldValue))
			{
				byte[] buffer = System.Convert.FromBase64String(field.FieldValue);
				itemField.SetBlobStream(new MemoryStream(buffer, false));

				_logger.WroteBlobStream(item, field);
			}
			else if (!field.FieldValue.Equals(itemField.Value))
			{
				var oldValue = itemField.Value;
				itemField.SetValue(field.FieldValue, true);
				_logger.UpdatedChangedFieldValue(item, field, oldValue);
			}
		}

		/// <summary>
		/// Removes information about a specific item from database caches. This compensates
		/// cache functionality that depends on database events (which are disabled when loading).
		/// </summary>
		/// <param name="database">Database to clear caches for.</param>
		/// <param name="itemId">Item ID to remove</param>
		protected virtual void ClearCaches(Database database, ID itemId)
		{
			database.Caches.ItemCache.RemoveItem(itemId);
			database.Caches.DataCache.RemoveItemInformation(itemId);
		}

		protected virtual void ResetTemplateEngineIfItemIsTemplate(Item target)
		{
			if (!target.Database.Engines.TemplateEngine.IsTemplatePart(target))
				return;

			target.Database.Engines.TemplateEngine.Reset();
		}

		/// <summary>
		/// Asserts that the template is present in the database.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="templateId">The template.</param>
		/// <returns>
		/// The template being asserted.
		/// </returns>
		protected virtual Template AssertTemplate(Database database, ID templateId)
		{
			Template template = database.Engines.TemplateEngine.GetTemplate(templateId);
			if (template == null)
			{
				database.Engines.TemplateEngine.Reset();
				template = database.Engines.TemplateEngine.GetTemplate(templateId);
			}
			Assert.IsNotNull(template, "Template: " + templateId + " not found");
			return template;
		}
	}
}
