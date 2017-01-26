using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;

// ReSharper disable TooWideLocalVariableScope

namespace Unicorn.Data.Dilithium.Data
{
	public class DataCore
	{
		private Dictionary<Guid, DilithiumItemData> _itemsById; // start with large capacity. Dictionary likes primes for its size.
		private Dictionary<string, IList<Guid>> _itemsByPath;
		private readonly Dictionary<Guid, TemplateField> _templateMetadataLookup = new Dictionary<Guid, TemplateField>(1000);

		public DataCore(string databaseName)
		{
			Database = Database.GetDatabase(databaseName);
			Assert.ArgumentNotNull(Database, nameof(databaseName));
		}

		public Database Database { get; }
		public int Count => _itemsById.Count;

		public IEnumerable<DilithiumItemData> GetChildren(DilithiumItemData item)
		{
			return GuidsToItems(item.Children);
		}

		public IEnumerable<DilithiumItemData> GetByPath(string path)
		{
			IList<Guid> itemsAtPath;

			if(!_itemsByPath.TryGetValue(path, out itemsAtPath)) return new DilithiumItemData[0];

			return GuidsToItems(itemsAtPath);
		}

		public DilithiumItemData GetById(Guid id)
		{
			DilithiumItemData item;
			if (_itemsById.TryGetValue(id, out item)) return item;

			return null;
		} 

		public void Ingest(SqlDataReader reader, IList<DilithiumReactor.RootData> rootData)
		{
			IngestItemData(reader);

			var readDataTask = Task.Run(() => IngestFieldData(reader, false));

			IndexChildren();
			IndexPaths(rootData);

			Task.WaitAll(readDataTask);
		}

		private void IngestItemData(SqlDataReader reader)
		{
			// 8087 = prime. Lots of items will load in so we start with a large capacity to minimize expansions.
			// Dictionary expands using primes, hence our choice.
			var results = new Dictionary<Guid, DilithiumItemData>(8087);

			DilithiumItemData currentItem;
			while (reader.Read())
			{
				// NOTE: refer to SQL in Reactor (first query) to get column ordinals
				currentItem = new DilithiumItemData(this);
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

		private void IngestFieldData(SqlDataReader reader, bool secondPass)
		{
			// the reader will be on result set 0 when it arrives (item data)
			// so we need to advance it to set 1 (descendants field data)
			reader.NextResult();

			var itemsById = _itemsById;
			Guid itemId;
			string language;
			int version;
			DilithiumFieldValue currentField;
			DilithiumItemData targetItem;

			while (reader.Read())
			{
				itemId = reader.GetGuid(0);

				currentField = new DilithiumFieldValue(itemId, Database.Name);

				language = reader.GetString(1);
				currentField.FieldId = reader.GetGuid(2);
				currentField.Value = reader.GetString(3);
				version = reader.GetInt32(4);

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
					SetSharedField(targetItem, currentField, version, language);
				}
				else if (fieldMetadata.IsUnversioned)
				{
					// unversioned field = no version, with language (version -1 is used as a nonversioned flag)
					SetUnversionedField(targetItem, currentField, version, language);
				}
				else
				{
					// versioned field
					SetVersionedField(targetItem, language, version, currentField);
				}
			}

			// the third result in the reader is the root item fields.
			// this has an identical schema to the descendant fields and thus this method can be used to parse it as well.
			if (!secondPass) IngestFieldData(reader, true);
		}

		private void IndexPaths(IList<DilithiumReactor.RootData> rootData)
		{
			DilithiumItemData currentItem;
			IList<Guid> pathItemList;
			IEnumerable<DilithiumItemData> childList;

			var processQueue = new Queue<DilithiumItemData>(_itemsById.Count);

			// seed the queue with the known roots, setting their paths
			// all other items will be pathed up based on these
			foreach (var root in rootData)
			{
				currentItem = _itemsById[root.Id];
				currentItem.Path = root.Path;

				processQueue.Enqueue(currentItem);
			}

			var pathIndex = new Dictionary<string, IList<Guid>>(StringComparer.Ordinal);

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
			DilithiumItemData currentItem;
			DilithiumItemData parentItem;
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

		private IList<DilithiumItemData> GuidsToItems(IList<Guid> guids)
		{
			var items = new List<DilithiumItemData>(guids.Count);

			foreach (var childId in guids)
			{
				items.Add(_itemsById[childId]);
			}

			return items;
		}

		private void SetSharedField(DilithiumItemData targetItem, DilithiumFieldValue currentField, int version, string language)
		{
			// check for corruption in SQL server tables (field values in wrong table) - shared field should have neither language nor version one or greater (SQL sends version -1 for shared)
			if (version >= 1)
			{
				Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}:{targetItem.Id}! Field {currentField.FieldId} (shared) had a value in the versioned fields table. The field value will be ignored.", this);
				return;
			}

			if (!string.IsNullOrEmpty(language))
			{
				Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}:{targetItem.Id}! Field {currentField.FieldId} (shared) had a value in the unversioned fields table. The field value will be ignored.", this);
				return;
			}

			targetItem.RawSharedFields.Add(currentField);
		}

		private void SetUnversionedField(DilithiumItemData targetItem, DilithiumFieldValue currentField, int version, string language)
		{
			// check for corruption in SQL server tables (field values in wrong table) - an unversioned field should have a version less than 1 (SQL sends -1 back for unversioned) and a language
			if (version >= 1)
			{
				Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}:{targetItem.Id}! Field {currentField.FieldId} (unversioned) had a value in the versioned fields table. The field value will be ignored.", this);
				return;
			}

			if (string.IsNullOrEmpty(language))
			{
				Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}:{targetItem.Id}! Field {currentField.FieldId} (unversioned) had a value in the shared fields table. The field value will be ignored.", this);
				return;
			}

			foreach (var languageFields in targetItem.RawUnversionedFields)
			{
				if (languageFields.Language.Name.Equals(language, StringComparison.Ordinal))
				{
					languageFields.RawFields.Add(currentField);
					return;
				}
			}

			var newLanguage = new DilithiumItemLanguage();
			newLanguage.Language = new CultureInfo(language);
			newLanguage.RawFields.Add(currentField);

			targetItem.RawUnversionedFields.Add(newLanguage);
		}

		private void SetVersionedField(DilithiumItemData targetItem, string language, int version, DilithiumFieldValue currentField)
		{
			// check for corruption in SQL server tables (field values in wrong table) - a versioned field should have both a language and a version that's one or greater
			if (version < 1)
			{
				if (string.IsNullOrEmpty(language))
				{
					Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}:{targetItem.Id}! Field {currentField.FieldId} (versioned) had a value in the unversioned fields table. The field value will be ignored.", this);
				}
				else
				{
					Log.Error($"[Dilithium] Data corruption in {targetItem.DatabaseName}:{targetItem.Id}! Field {currentField.FieldId} (versioned) had a value in the shared fields table. The field value will be ignored.", this);
				}
				return;
			}

			foreach (var versionFields in targetItem.RawVersions)
			{
				if (versionFields.Language.Name.Equals(language, StringComparison.Ordinal) && versionFields.VersionNumber == version)
				{
					versionFields.RawFields.Add(currentField);
					return;
				}
			}

			var newVersion = new DilithiumItemVersion();
			newVersion.Language = new CultureInfo(language);
			newVersion.VersionNumber = version;
			newVersion.RawFields.Add(currentField);

			targetItem.RawVersions.Add(newVersion);
		}
	}
}
