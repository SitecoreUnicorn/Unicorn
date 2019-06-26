using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;

// ReSharper disable TooWideLocalVariableScope

namespace Unicorn.Data.Dilithium.Sql
{
	/// <summary>
	/// Stores and indexes cached items from a MSSQL database. Each SqlDataCore can store items from one Sitecore database.
	/// </summary>
	public class SqlDataCache
	{
		private Dictionary<Guid, SqlItemData> _itemsById; // start with large capacity. Dictionary likes primes for its size.
		private Dictionary<string, IList<Guid>> _itemsByPath;
		private readonly Dictionary<Guid, TemplateField> _templateMetadataLookup = new Dictionary<Guid, TemplateField>(1000);

		public SqlDataCache(string databaseName)
		{
			Database = Database.GetDatabase(databaseName);
			Assert.ArgumentNotNull(Database, nameof(databaseName));
		}

		public Database Database { get; }
		public int Count => _itemsById.Count;

		public void Update(IItemData updatedItem)
		{
			if (_itemsById.ContainsKey(updatedItem.Id))
			{
				// update essential item cached data in the ID cache
				var existingItem = _itemsById[updatedItem.Id];
				existingItem.ParentId = updatedItem.ParentId;
				existingItem.Path = updatedItem.Path;
				existingItem.Name = updatedItem.Name;

				// update path data
				if (_itemsByPath.ContainsKey(existingItem.Path))
				{
					_itemsByPath[existingItem.Path].Remove(existingItem.Id);
				}

				if(!_itemsByPath.ContainsKey(updatedItem.Path)) _itemsByPath.Add(updatedItem.Path, new List<Guid>());

				_itemsByPath[updatedItem.Path].Add(updatedItem.Id);
			}
		}

		public void Remove(IItemData removedItem)
		{
			if (_itemsById.ContainsKey(removedItem.Id))
			{
				var existingItem = _itemsById[removedItem.Id];

				_itemsById.Remove(existingItem.Id);

				_itemsByPath.Remove(existingItem.Path);
			}
		}

		public IList<SqlItemData> GetChildren(SqlItemData item)
		{
			return GuidsToItems(item.Children);
		}

		public IList<SqlItemData> GetByPath(string path)
		{
			IList<Guid> itemsAtPath;

			if(!_itemsByPath.TryGetValue(path, out itemsAtPath)) return new SqlItemData[0];

			return GuidsToItems(itemsAtPath);
		}

		public SqlItemData GetById(Guid id)
		{
			SqlItemData item;
			if (_itemsById.TryGetValue(id, out item)) return item;

			return null;
		} 

		public bool Ingest(SqlDataReader reader, IList<SqlPrecacheStore.RootData> rootData)
		{
			IngestItemData(reader);

			var readDataTask = Task.Run(() => IngestFieldData(reader));

			IndexChildren();
			IndexPaths(rootData);

			readDataTask.Wait();

			return !readDataTask.Result;
		}

		private void IngestItemData(SqlDataReader reader)
		{
			// 8087 = prime. Lots of items will load in so we start with a large capacity to minimize expansions.
			// Dictionary expands using primes, hence our choice.
			var results = new Dictionary<Guid, SqlItemData>(8087);

			SqlItemData currentItem;
			while (reader.Read())
			{
				// NOTE: refer to SQL in Reactor (first query) to get column ordinals
				currentItem = new SqlItemData(this);
				currentItem.Id = reader.GetGuid(0);
				currentItem.Name = reader.GetString(1);
				currentItem.TemplateId = reader.GetGuid(2);
				currentItem.BranchId = reader.GetGuid(3);
				currentItem.ParentId = reader.GetGuid(4);
				currentItem.DatabaseName = Database.Name;

				results.Add(currentItem.Id, currentItem);
			}

			_itemsById = results;
		}

		private bool IngestFieldData(SqlDataReader reader)
		{
			bool errors = false;

			// the reader will be on result set 0 when it arrives (item data)
			// so we need to advance it to set 1 (descendants field data)
			reader.NextResult();

			var itemsById = _itemsById;
			Guid itemId;
			string language;
			int version;
			SqlItemFieldValue currentField;
			SqlItemData targetItem;

			while (reader.Read())
			{
				itemId = reader.GetGuid(0);
				language = reader.GetString(1);
				version = reader.GetInt32(4);

				currentField = new SqlItemFieldValue(itemId, Database.Name, language, version);

				currentField.FieldId = reader.GetGuid(2);
				currentField.Value = reader.GetString(3);

				// get current item to add fields to
				if (!itemsById.TryGetValue(itemId, out targetItem)) throw new InvalidOperationException($"Item {itemId} was not read by the item loader but had field {currentField.FieldId} in the field loader!");

				var fieldMetadata = GetTemplateField(currentField.FieldId, targetItem.TemplateId);

				if (fieldMetadata == null)
				{
					// if we got here it probably means that there's a field value in the DB that is from the _wrong_ template ID
					// Sitecore seems to ignore this when it occurs, so so will we - we'll skip loading the field
					continue;
				}

				currentField.NameHint = fieldMetadata.Name;
				currentField.FieldType = fieldMetadata.Type;

				// for blob fields we need to set the blob ID so it can be read
				if (fieldMetadata.IsBlob)
				{
					string blobCandidateValue = currentField.Value;
					if (blobCandidateValue.Length > 38)
						blobCandidateValue = blobCandidateValue.Substring(0, 38);

					ID blobId;
					if (ID.TryParse(blobCandidateValue, out blobId)) currentField.BlobId = blobId.Guid;
				}

				// add field to target item data
				if (fieldMetadata.IsShared)
				{
					// shared field = no version, no language
					if (!SetSharedField(targetItem, currentField, version, language)) errors = true;
				}
				else if (fieldMetadata.IsUnversioned)
				{
					// unversioned field = no version, with language (version -1 is used as a nonversioned flag)
					if (!SetUnversionedField(targetItem, currentField, version, language)) errors = true;
				}
				else
				{
					// versioned field
					if (!SetVersionedField(targetItem, language, version, currentField)) errors = true;
				}
			}

			return errors;
		}

		private void IndexPaths(IList<SqlPrecacheStore.RootData> rootData)
		{
			SqlItemData currentItem;
			IList<Guid> pathItemList;
			IEnumerable<SqlItemData> childList;

			var processQueue = new Queue<SqlItemData>(_itemsById.Count);

			// seed the queue with the known roots, setting their paths
			// all other items will be pathed up based on these
			foreach (var root in rootData)
			{
				// we still have to make sure the root is defined in the data
				// e.g. a root that had transparent sync on could 'resolve' to an item + ID
				// yet not actually be in the DB/part of the data in results
				if (_itemsById.TryGetValue(root.Id, out currentItem))
				{
					currentItem.Path = root.Path;
					processQueue.Enqueue(currentItem);
				}
			}

			var pathIndex = new Dictionary<string, IList<Guid>>(StringComparer.OrdinalIgnoreCase);

			while (processQueue.Count > 0)
			{
				// index current item
				currentItem = processQueue.Dequeue();

				if (!pathIndex.TryGetValue(currentItem.Path, out pathItemList))
				{
					pathItemList = pathIndex[currentItem.Path] = new List<Guid>();
				}

				pathItemList.Add(currentItem.Id);

				// path children and enqueue for further processing
				childList = GetChildren(currentItem);
				foreach (var child in childList)
				{
					child.Path = $"{currentItem.Path}/{child.Name}";
					processQueue.Enqueue(child);
				}
			}

			_itemsByPath = pathIndex;
		}

		private void IndexChildren()
		{
			SqlItemData currentItem;
			SqlItemData parentItem;
			var itemsById = _itemsById;

			// puts the IDs of all children into each item's Children list
			foreach (var itemById in itemsById)
			{
				currentItem = itemById.Value;

				if (itemsById.TryGetValue(currentItem.ParentId, out parentItem))
				{
					parentItem.Children.Add(currentItem.Id);
				}
			}
		}

		private TemplateField GetTemplateField(Guid fieldId, Guid templateId)
		{
			TemplateField result;
			if (_templateMetadataLookup.TryGetValue(fieldId, out result)) return result;

			var candidateField = TemplateManager.GetTemplateField(new ID(fieldId), new ID(templateId), Database);

			if (candidateField != null) return _templateMetadataLookup[fieldId] = candidateField;

			// if we got here it probably means that there's a field value in the DB that is from the _wrong_ template ID
			// Sitecore seems to ignore this when it occurs, so so will we - we'll skip loading the field
			return null;
		}

		private IList<SqlItemData> GuidsToItems(IList<Guid> guids)
		{
			var items = new List<SqlItemData>(guids.Count);

			foreach (var childId in guids)
			{
				if (!_itemsById.ContainsKey(childId))
				{
					Log.Warn($"[Dilithium] The child ID {childId} was not present in the cached data. If this item was removed during the sync this is normal. If it was not, there might be a problem or bug.", this);
					continue;
				}

				items.Add(_itemsById[childId]);
			}

			return items;
		}

		private bool SetSharedField(SqlItemData targetItem, SqlItemFieldValue currentField, int version, string language)
		{
			// check for corruption in SQL server tables (field values in wrong table) - shared field should have neither language nor version one or greater (SQL sends version -1 for shared)
			if (version >= 1)
			{
				Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}://{{{targetItem.Id}}}! Field {{{currentField.FieldId}}} (shared) had a value in the versioned fields table. The field value will be ignored.", this);
				return false;
			}

			if (!string.IsNullOrEmpty(language))
			{
				Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}://{{{targetItem.Id}}}! Field {currentField.FieldId} (shared) had a value in the unversioned fields table. The field value will be ignored.", this);
				return false;
			}

			targetItem.RawSharedFields.Add(currentField);

			return true;
		}

		private bool SetUnversionedField(SqlItemData targetItem, SqlItemFieldValue currentField, int version, string language)
		{
			// check for corruption in SQL server tables (field values in wrong table) - an unversioned field should have a version less than 1 (SQL sends -1 back for unversioned) and a language
			if (version >= 1)
			{
				Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}://{{{targetItem.Id}}}! Field {currentField.FieldId} (unversioned) had a value in the versioned fields table. The field value will be ignored.", this);
				return false;
			}

			if (string.IsNullOrEmpty(language))
			{
				Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}://{{{targetItem.Id}}}! Field {currentField.FieldId} (unversioned) had a value in the shared fields table. The field value will be ignored.", this);
				return false;
			}

			foreach (var languageFields in targetItem.RawUnversionedFields)
			{
				if (languageFields.Language.Name.Equals(language, StringComparison.Ordinal))
				{
					languageFields.RawFields.Add(currentField);
					return true;
				}
			}

			var newLanguage = new SqlItemLanguage();
			newLanguage.Language = new CultureInfo(language);
			newLanguage.RawFields.Add(currentField);

			targetItem.RawUnversionedFields.Add(newLanguage);

			return true;
		}

		private bool SetVersionedField(SqlItemData targetItem, string language, int version, SqlItemFieldValue currentField)
		{
			// check for corruption in SQL server tables (field values in wrong table) - a versioned field should have both a language and a version that's one or greater
			if (version < 1)
			{
				if (string.IsNullOrEmpty(language))
				{
					Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}://{{{targetItem.Id}}}! Field {currentField.FieldId} (versioned) had a value in the shared fields table. The field value will be ignored.", this);
				}
				else
				{
					Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}://{{{targetItem.Id}}}! Field {currentField.FieldId} (versioned) had a value in the unversioned fields table. The field value will be ignored.", this);
				}
				return false;
			}

			foreach (var versionFields in targetItem.RawVersions)
			{
				if (versionFields.Language.Name.Equals(language, StringComparison.Ordinal) && versionFields.VersionNumber == version)
				{
					versionFields.RawFields.Add(currentField);
					return true;
				}
			}

			var newVersion = new SqlItemVersion();
			newVersion.Language = new CultureInfo(language);
			newVersion.VersionNumber = version;
			newVersion.RawFields.Add(currentField);

			targetItem.RawVersions.Add(newVersion);

			return true;
		}
	}
}
