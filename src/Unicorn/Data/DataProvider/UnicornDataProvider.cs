using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rainbow.Filtering;
using Rainbow.Model;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Unicorn.Predicates;
using ItemData = Rainbow.Storage.Sc.ItemData;

namespace Unicorn.Data.DataProvider
{
	/// <summary>
	/// This class provides event-handling services to Unicorn - reflecting actions onto the serialization provider via the predicate when
	/// changes occur to the source data.
	/// </summary>
	public class UnicornDataProvider : IDisposable
	{
		public const string TransparentSyncUpdatedByValue = "serialization\\UnicornDataProvider";

		private readonly ITargetDataStore _targetDataStore;
		private readonly ISourceDataStore _sourceDataStore;
		private readonly IPredicate _predicate;
		private readonly IFieldFilter _fieldFilter;
		private readonly IUnicornDataProviderLogger _logger;
		private readonly IUnicornDataProviderConfiguration _configuration;
		private static bool _disableSerialization;
		private bool _disableTransparentSync;
		private readonly Dictionary<Guid, Tuple<string, Guid>> _blobIdLookup = new Dictionary<Guid, Tuple<string, Guid>>();

		public UnicornDataProvider(ITargetDataStore targetDataStore, ISourceDataStore sourceDataStore, IPredicate predicate, IFieldFilter fieldFilter, IUnicornDataProviderLogger logger, IUnicornDataProviderConfiguration configuration)
		{
			Assert.ArgumentNotNull(targetDataStore, "serializationProvider");
			Assert.ArgumentNotNull(predicate, "predicate");
			Assert.ArgumentNotNull(fieldFilter, "fieldPredicate");
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(sourceDataStore, "sourceDataStore");
			Assert.ArgumentNotNull(configuration, "configuration");

			_logger = logger;
			_configuration = configuration;
			_predicate = predicate;
			_fieldFilter = fieldFilter;
			_targetDataStore = targetDataStore;
			_sourceDataStore = sourceDataStore;

			try
			{
				_targetDataStore.RegisterForChanges(RemoveItemFromCaches);
			}
			catch (NotImplementedException)
			{
				// if the data store doesn't implement watching, cool story bruv
			}
		}

		/// <summary>
		/// Disables all serialization handling if true. Used during serialization load tasks.
		/// </summary>
		public static bool DisableSerialization
		{
			get
			{
				// we have to check standard serialization's disabled attribute as well,
				// because if we do not serialization operations in the content editor will clash with ours
				if (ItemHandler.DisabledLocally) return true;
				return _disableSerialization;
			}
			set { _disableSerialization = value; }
		}

		/// <summary>
		/// Disables transparent sync (reading from the target data store)
		/// This is appropriate for large data sets, or slower data providers.
		/// </summary>
		public bool DisableTransparentSync
		{
			get
			{
				if (TransparentSyncDisabler.CurrentValue) return true;
				if (!_configuration.EnableTransparentSync) return true;
				return _disableTransparentSync;
			}
			set { _disableTransparentSync = value; }
		}

		public Sitecore.Data.DataProviders.DataProvider ParentDataProvider { get; set; }

		protected Database Database { get { return ParentDataProvider.Database; } }

		public void CreateItem(ItemDefinition newItem, ID templateId, ItemDefinition parent, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(newItem, "itemDefinition");

			SerializeItemIfIncluded(newItem, "Created");
		}

		public void SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
			Assert.ArgumentNotNull(changes, "changes");

			var sourceItem = GetSourceFromId(changes.Item.ID);

			if (sourceItem == null) return;

			if (!_predicate.Includes(sourceItem).IsIncluded) return;

			string oldName = changes.Renamed ? changes.Properties["name"].OriginalValue.ToString() : string.Empty;
			if (changes.Renamed && !oldName.Equals(sourceItem.Name, StringComparison.Ordinal))
			// it's a rename, in which the name actually changed (template builder will cause 'renames' for the same name!!!)
			{
				using (new DatabaseCacheDisabler())
				{
					// disabling the DB caches while running this ensures that any children of the renamed item are retrieved with their proper post-rename paths and thus are not saved at their old location

					// this allows us to filter out any excluded children by predicate when the data store moves children
					var predicatedItem = new PredicateFilteredItemData(sourceItem, _predicate);

					_targetDataStore.MoveOrRenameItem(predicatedItem, changes.Item.Paths.ParentPath + "/" + oldName);
				}

				_logger.RenamedItem(_targetDataStore.FriendlyName, sourceItem, oldName);
			}
			else if (HasConsequentialChanges(changes))
			// it's a simple update - but we reject it if only inconsequential fields (last updated, revision) were changed - again, template builder FTW
			{
				_targetDataStore.Save(sourceItem);

				AddBlobsToCache(sourceItem);

				_logger.SavedItem(_targetDataStore.FriendlyName, sourceItem, "Saved");
			}
		}

		public void MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var oldSourceItem = GetSourceFromId(itemDefinition.ID, true); // we use cache here because we want the old path

			var oldPath = oldSourceItem.Path; // NOTE: we cap the path here, because once we enter the cache-disabled section - to get the new paths for parent and children - the path cache updates and the old path is lost in oldSourceItem because it is reevaluated each time.

			var destinationItem = GetSourceFromId(destination.ID);

			if (!_predicate.Includes(destinationItem).IsIncluded) // if the destination we are moving to is NOT included for serialization, we delete the existing item
			{
				var existingItem = _targetDataStore.GetByPathAndId(oldSourceItem.Path, oldSourceItem.Id, oldSourceItem.DatabaseName);

				if (existingItem != null)
				{
					_targetDataStore.Remove(existingItem);
					_logger.MovedItemToNonIncludedLocation(_targetDataStore.FriendlyName, existingItem);
				}

				return;
			}

			using (new DatabaseCacheDisabler())
			{
				// disabling the DB caches while running this ensures that any children of the moved item are retrieved with their proper post-rename paths and thus are not saved at their old location

				var sourceItem = GetSourceFromId(itemDefinition.ID); // re-get the item with cache disabled

				// this allows us to filter out any excluded children by predicate when the data store moves children
				var predicatedItem = new PredicateFilteredItemData(sourceItem, _predicate);

				_targetDataStore.MoveOrRenameItem(predicatedItem, oldPath);
				_logger.MovedItem(_targetDataStore.FriendlyName, sourceItem, destinationItem);
			}
		}

		public void CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyId, CallContext context)
		{
			if (DisableSerialization) return;

			// copying is easy - all we have to do is serialize the copyID. Copied children will all result in multiple calls to CopyItem so we don't even need to worry about them.
			var copiedItem = new ItemData(Database.GetItem(copyId), _sourceDataStore);

			if (!_predicate.Includes(copiedItem).IsIncluded) return; // destination parent is not in a path that we are serializing, so skip out

			_targetDataStore.Save(copiedItem);
			_logger.CopiedItem(_targetDataStore.FriendlyName, () => GetSourceFromId(source.ID), copiedItem);
		}

		public void AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			SerializeItemIfIncluded(itemDefinition, "Version Added");
		}

		public void DeleteItem(ItemDefinition itemDefinition, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var existingItem = GetSourceFromId(itemDefinition.ID, true);

			if (existingItem == null) return; // it was already gone or an item from a different data provider

			if (_targetDataStore.Remove(existingItem))
				_logger.DeletedItem(_targetDataStore.FriendlyName, existingItem);
		}

		public void RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			SerializeItemIfIncluded(itemDefinition, "Version Removed");
		}

		public void RemoveVersions(ItemDefinition itemDefinition, Language language, bool removeSharedData, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			SerializeItemIfIncluded(itemDefinition, "Versions Removed");
		}

		public virtual IEnumerable<ID> GetChildIds(ItemDefinition itemDefinition, CallContext context)
		{
			if (DisableSerialization || DisableTransparentSync) return Enumerable.Empty<ID>();

			// expectation: do not return null, return empty enumerable for not included etc
			var parentItem = GetTargetFromId(itemDefinition.ID);

			if (parentItem == null) return Enumerable.Empty<ID>();

			return _targetDataStore.GetChildren(parentItem).Select(item => new ID(item.Id));
		}

		public virtual ItemDefinition GetItemDefinition(ID itemId, CallContext context)
		{
			if (DisableSerialization || DisableTransparentSync) return null;

			// return null if not present
			var item = GetTargetFromId(itemId);

			if (item == null) return null;

			return new ItemDefinition(new ID(item.Id), item.Name, new ID(item.TemplateId), new ID(item.BranchId));
		}

		public virtual FieldList GetItemFields(ItemDefinition itemDefinition, VersionUri versionUri, CallContext context)
		{
			// return null if not present, should return all shared fields as well as all fields in the version
			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
			Assert.ArgumentNotNull(versionUri, "versionUri");

			if (DisableSerialization || DisableTransparentSync || itemDefinition == ItemDefinition.Empty) return null;

			var item = GetTargetFromId(itemDefinition.ID);

			if (item == null) return null;

			var fields = new FieldList();

			foreach (var sharedField in item.SharedFields)
			{
				fields.Add(new ID(sharedField.FieldId), sharedField.BlobId.HasValue ? sharedField.BlobId.ToString() : sharedField.Value);
			}

			var version = item.Versions.FirstOrDefault(v => v.VersionNumber == versionUri.Version.Number && v.Language.Name == versionUri.Language.Name);

			if (version == null) return fields;

			foreach (var versionedField in version.Fields)
			{
				fields.Add(new ID(versionedField.FieldId), versionedField.BlobId.HasValue ? versionedField.BlobId.ToString() : versionedField.Value);
			}

			fields.Add(FieldIDs.UpdatedBy, TransparentSyncUpdatedByValue);
			fields.Add(FieldIDs.Revision, Guid.NewGuid().ToString());

			AddBlobsToCache(item);

			return fields;
		}

		public virtual VersionUriList GetItemVersions(ItemDefinition itemDefinition, CallContext context)
		{
			// return null if not present
			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			if (DisableSerialization || DisableTransparentSync || itemDefinition == ItemDefinition.Empty) return null;

			var versions = new VersionUriList();

			var item = GetTargetFromId(itemDefinition.ID);

			if (item == null) return null;

			foreach (var version in item.Versions)
			{
				versions.Add(Language.Parse(version.Language.Name), Sitecore.Data.Version.Parse(version.VersionNumber));
			}

			return versions;
		}

		public virtual ID GetParentId(ItemDefinition itemDefinition, CallContext context)
		{
			// return null if not present
			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			if (DisableSerialization || DisableTransparentSync || itemDefinition == ItemDefinition.Empty) return null;

			var item = GetTargetFromId(itemDefinition.ID);

			return item != null ? new ID(item.ParentId) : null;
		}

		public virtual ID ResolvePath(string itemPath, CallContext context)
		{
			if (DisableSerialization || DisableTransparentSync) return null;

			// return null if not present
			var item = _targetDataStore.GetByPath(itemPath, Database.Name).FirstOrDefault();

			if (item == null || !_predicate.Includes(item).IsIncluded) return null;

			return new ID(item.Id);
		}

		public virtual bool? HasChildren(ItemDefinition itemDefinition, CallContext context)
		{
			// return null if not present
			if (DisableSerialization || DisableTransparentSync) return null;

			return GetChildIds(itemDefinition, context).Any();
		}

		public virtual IEnumerable<ID> GetTemplateItemIds(CallContext context)
		{
			if (DisableSerialization || DisableTransparentSync) return Enumerable.Empty<ID>();

			// expectation: do not return null, return empty enumerable for not included etc
			return _targetDataStore.GetMetadataByTemplateId(TemplateIDs.Template.Guid, Database.Name)
				.Select(template => new ID(template.Id));
		}

		public virtual Stream GetBlobStream(Guid blobId, CallContext context)
		{
			if (DisableSerialization || DisableTransparentSync) return null;

			return GetBlobFromCache(blobId);
		}

		public virtual bool BlobStreamExists(Guid blobId, CallContext context)
		{
			if (DisableSerialization || DisableTransparentSync) return false;

			return _blobIdLookup.ContainsKey(blobId);
		}

		protected virtual bool SerializeItemIfIncluded(ItemDefinition itemDefinition, string triggerReason)
		{
			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromId(itemDefinition.ID);

			if (!_predicate.Includes(sourceItem).IsIncluded) return false; // item was not included so we get out

			_targetDataStore.Save(sourceItem);
			_logger.SavedItem(_targetDataStore.FriendlyName, sourceItem, triggerReason);

			return true;
		}

		protected virtual bool HasConsequentialChanges(ItemChanges changes)
		{
			// properties, e.g. template, etc are always consequential
			// NOTE: sometimes you can get spurious 'changes' where the old and new value are the same. We reject those.
			if (changes.HasPropertiesChanged && changes.Properties.Any(x => !x.Value.OriginalValue.Equals(x.Value.Value))) return true;

			foreach (FieldChange change in changes.FieldChanges)
			{
				if (change.OriginalValue == change.Value) continue;
				if (change.FieldID == FieldIDs.Revision) continue;
				if (change.FieldID == FieldIDs.Updated) continue;
				if (change.FieldID == FieldIDs.UpdatedBy) continue;
				if (change.FieldID == FieldIDs.Originator) continue;
				if (!_fieldFilter.Includes(change.FieldID.Guid)) continue;

				return true;
			}

			_logger.SaveRejectedAsInconsequential(_targetDataStore.FriendlyName, changes);

			return false;
		}

		protected virtual IItemData GetSourceFromId(ID id, bool useCache = false)
		{
			var item = GetItemFromId(id, useCache);

			if (item == null) return null;

			return new ItemData(item);
		}

		/// <summary>
		/// When items are acquired from the data provider they can be stale in cache, which fouls up serializing them.
		/// Renames and template changes are particularly vulnerable to this.
		/// </summary>
		protected virtual Item GetItemFromId(ID id, bool useCache)
		{
			if (!useCache)
			{
				using (new TransparentSyncDisabler())
				{
					using (new DatabaseCacheDisabler())
					{
						return Database.GetItem(id);
					}
				}
			}

			return Database.GetItem(id);
		}

		/// <summary>
		/// Given an item definition, resolve it in the target data store
		/// This involves seeing if we can use the Sitecore db to resolve its path
		/// (as definition does not include the path) so we can efficiently look it up
		/// in path based data stores. If we cannot resolve a path, or the path is incorrect,
		/// we fall back to using GetById on the target data store.
		/// </summary>
		protected virtual IItemData GetTargetFromId(ID id)
		{
			return _targetDataStore.GetById(id.Guid, Database.Name);
		}

		protected virtual void AddBlobsToCache(IItemData itemData)
		{
			var blobFields = itemData.SharedFields.Concat(itemData.Versions.SelectMany(version => version.Fields))
				.Where(field => field.BlobId.HasValue)
				.ToArray();

			foreach (var field in blobFields)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				// ReSharper disable once PossibleInvalidOperationException
				_blobIdLookup[field.BlobId.Value] = Tuple.Create(itemData.Path, itemData.Id);
			}
		}

		protected virtual Stream GetBlobFromCache(Guid blobId)
		{
			Tuple<string, Guid> blobEntry;

			if (!_blobIdLookup.TryGetValue(blobId, out blobEntry)) return null;

			var targetItem = _targetDataStore.GetByPathAndId(blobEntry.Item1, blobEntry.Item2, Database.Name);
			if (targetItem == null) return null;
			var targetFieldValue = targetItem.SharedFields.Concat(targetItem.Versions.SelectMany(version => version.Fields))
					.FirstOrDefault(targetField => targetField.BlobId.HasValue && targetField.BlobId.Value == blobId);

			if (targetFieldValue == null || targetFieldValue.Value.Length == 0) return null;

			return new MemoryStream(System.Convert.FromBase64String(targetFieldValue.Value));
		}

		protected virtual void RemoveItemFromCaches(IItemMetadata metadata, string databaseName)
		{
			if (databaseName != Database.Name) return;

			// this is a bit heavy handed, sure.
			// but the caches get interdependent stuff - like caching child IDs
			// that make it difficult to cleanly remove a single item ID from all cases in the cache
			// either way, this should be a relatively rare occurrence (from runtime changes on disk)
			// and we're preserving prefetch, etc. Seems pretty zippy overall.
			Database.Caches.DataCache.Clear();
			Database.Caches.ItemCache.Clear();
			Database.Caches.ItemPathsCache.Clear();
			Database.Caches.PathCache.Clear();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// ReSharper disable once SuspiciousTypeConversion.Global
				var targetAsDisposable = _targetDataStore as IDisposable;
				if(targetAsDisposable != null) targetAsDisposable.Dispose();
			}
		}
	}
}
