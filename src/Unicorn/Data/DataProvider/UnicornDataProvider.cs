using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Rainbow.Filtering;
using Rainbow.Model;
using Sitecore;
using Sitecore.Collections;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using Sitecore.Data.Archiving;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using Sitecore.Globalization;
using Unicorn.Loader;
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
		private readonly IUnicornDataProviderConfiguration _dataProviderConfiguration;
		private readonly ISyncConfiguration _syncConfiguration;
		private readonly PredicateRootPathResolver _rootPathResolver;
		private static bool _disableSerialization;
		private bool _disableTransparentSync;
		private readonly ConcurrentDictionary<Guid, Tuple<string, Guid>> _blobIdLookup = new ConcurrentDictionary<Guid, Tuple<string, Guid>>();
		protected IReadOnlyDictionary<Guid, IReadOnlyList<ID>> RootIds;
		private readonly object _rootIdInitLock = new object();

		public UnicornDataProvider(ITargetDataStore targetDataStore, ISourceDataStore sourceDataStore, IPredicate predicate, IFieldFilter fieldFilter, IUnicornDataProviderLogger logger, IUnicornDataProviderConfiguration dataProviderConfiguration, ISyncConfiguration syncConfiguration, PredicateRootPathResolver rootPathResolver)
		{
			Assert.ArgumentNotNull(targetDataStore, nameof(targetDataStore));
			Assert.ArgumentNotNull(predicate, nameof(predicate));
			Assert.ArgumentNotNull(fieldFilter, nameof(fieldFilter));
			Assert.ArgumentNotNull(logger, nameof(logger));
			Assert.ArgumentNotNull(sourceDataStore, nameof(sourceDataStore));
			Assert.ArgumentNotNull(dataProviderConfiguration, nameof(dataProviderConfiguration));
			Assert.ArgumentNotNull(rootPathResolver, nameof(rootPathResolver));
			Assert.ArgumentNotNull(syncConfiguration, nameof(syncConfiguration));

			_logger = logger;
			_dataProviderConfiguration = dataProviderConfiguration;
			_syncConfiguration = syncConfiguration;
			_rootPathResolver = rootPathResolver;
			_predicate = predicate;
			_fieldFilter = fieldFilter;
			_targetDataStore = targetDataStore;
			_sourceDataStore = sourceDataStore;

			// enable capturing recycle bin and archive restores to serialize the target item if included
			EventManager.Subscribe<RestoreItemCompletedEvent>(HandleItemRestored);

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
				if (SerializationEnabler.CurrentValue) return false;

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
				if (!_dataProviderConfiguration.EnableTransparentSync) return true;
				return _disableTransparentSync;
			}
			set { _disableTransparentSync = value; }
		}

		public Sitecore.Data.DataProviders.DataProvider ParentDataProvider { get; set; }

		protected Database Database => ParentDataProvider.Database;

		public virtual void CreateItem(ItemDefinition newItem, ID templateId, ItemDefinition parent, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(newItem, "itemDefinition");

			// get the item's parent. We need to know the path to be able to serialize it, and the ItemDefinition doesn't have it.
			var parentItem = Database.GetItem(parent.ID);

			Assert.IsNotNull(parentItem, "New item parent {0} did not exist!", parent.ID);

			// create a new skeleton item based on the ItemDefinition; at this point the item has no fields, data, etc
			var newItemProxy = new ProxyItem(newItem.Name, newItem.ID.Guid, parent.ID.Guid, newItem.TemplateID.Guid, parentItem.Paths.Path + "/" + newItem.Name, Database.Name);
			newItemProxy.BranchId = newItem.BranchId.Guid;

			// serialize the skeleton if the predicate includes it
			SerializeItemIfIncluded(newItemProxy, "Created");
		}

		public virtual void SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
			Assert.ArgumentNotNull(changes, "changes");

			// get the item we're saving to evaluate with the predicate
			// NOTE: the item in this state may be incomplete as Sitecore can sometimes send partial item data and rely on changes to do the save
			// e.g. during package installations. So we have to merge the changes with any existing item data if we save it later, to keep it consistent.
			IItemData sourceItem = new ItemData(changes.Item);

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
				var existingSerializedItem = _targetDataStore.GetByPathAndId(sourceItem.Path, sourceItem.Id, sourceItem.DatabaseName);

				// generated an IItemData from the item changes we received, and apply those changes to the existing serialized item if any
				if (existingSerializedItem != null) sourceItem = new ItemChangeApplyingItemData(existingSerializedItem, changes);
				else sourceItem = new ItemChangeApplyingItemData(changes);

				_targetDataStore.Save(sourceItem);

				AddBlobsToCache(sourceItem);

				_logger.SavedItem(_targetDataStore.FriendlyName, sourceItem, "Saved");
			}
		}

		public virtual void MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromId(itemDefinition.ID, true); // we use cache here because we want the old path (no cache would have the new path); TpSync always has old path

			var oldPath = sourceItem.Path; // NOTE: we cap the path here, because Sitecore can change the item's path value as we're updating stuff.

			var destinationItem = GetSourceFromId(destination.ID);

			if (destinationItem == null) return; // can occur with TpSync on, when this isn't the configuration we're moving for the data store will return null

			if (!_predicate.Includes(destinationItem).IsIncluded) // if the destination we are moving to is NOT included for serialization, we delete the existing item
			{
				var existingItem = _targetDataStore.GetByPathAndId(sourceItem.Path, sourceItem.Id, sourceItem.DatabaseName);

				if (existingItem != null)
				{
					_targetDataStore.Remove(existingItem);
					_logger.MovedItemToNonIncludedLocation(_targetDataStore.FriendlyName, existingItem);
				}

				return;
			}

			// rebase the path to the new destination path (this handles children too)
			var rebasedSourceItem = new PathRebasingProxyItem(sourceItem, destinationItem.Path, destinationItem.Id);

			// this allows us to filter out any excluded children by predicate when the data store moves children
			var predicatedItem = new PredicateFilteredItemData(rebasedSourceItem, _predicate);

			_targetDataStore.MoveOrRenameItem(predicatedItem, oldPath);
			_logger.MovedItem(_targetDataStore.FriendlyName, predicatedItem, destinationItem);
		}

		public virtual void CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyId, CallContext context)
		{
			if (DisableSerialization) return;

			// copying is easy - all we have to do is serialize the item using the copyID and new target destination as the parent. 
			// Copied children will all result in multiple calls to CopyItem so we don't even need to worry about them.
			var existingItem = GetSourceFromId(source.ID, true);

			Assert.IsNotNull(existingItem, "Existing item to copy was not in the database!");

			var destinationItem = GetSourceFromId(destination.ID, true);

			Assert.IsNotNull(destinationItem, "Copy destination was not in the database!");

			// wrap the existing item in a proxy so we can mutate it into a copy
			var copyTargetItem = new ProxyItem(existingItem);

			copyTargetItem.Path = $"{destinationItem.Path}/{copyName}";
			copyTargetItem.Name = copyName;
			copyTargetItem.Id = copyId.Guid;
			copyTargetItem.ParentId = destination.ID.Guid;

			if (!_predicate.Includes(copyTargetItem).IsIncluded) return; // destination parent is not in a path that we are serializing, so skip out

			_targetDataStore.Save(copyTargetItem);
			_logger.CopiedItem(_targetDataStore.FriendlyName, existingItem, copyTargetItem);
		}

		public void ChangeTemplate(ItemDefinition itemDefinition, TemplateChangeList changeList, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, nameof(itemDefinition));
			Assert.ArgumentNotNull(changeList, nameof(changeList));

			var sourceItem = GetSourceFromId(itemDefinition.ID);

			var existingSerializedItem = _targetDataStore.GetByPathAndId(sourceItem.Path, sourceItem.Id, sourceItem.DatabaseName);

			if (existingSerializedItem == null) return;

			var newItem = new ProxyItem(sourceItem); // note: sourceItem gets dumped. Because it has field changes made to it.
			newItem.TemplateId = changeList.Target.ID.Guid;

			_targetDataStore.Save(newItem);

			_logger.SavedItem(_targetDataStore.FriendlyName, sourceItem, "TemplateChanged");
		}

		public virtual void AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromIdIfIncluded(itemDefinition);

			if (sourceItem == null) return; // not an included item

			// we make a clone of the item so that we can insert a new version on it
			var versionAddProxy = new ProxyItem(sourceItem);

			// determine what the next version number should be in the current language
			// (highest number currently present + 1)
			var newVersionNumber = 1 + versionAddProxy.Versions
				.Where(v => v.Language.Equals(baseVersion.Language.CultureInfo))
				.Select(v => v.VersionNumber)
				.DefaultIfEmpty()
				.Max();

			IItemVersion newVersion;

			// if the base version is 0 or less, that means add a blank version (per SC DP behavior). If 1 or more we should copy all fields on that version into the new version.
			if (baseVersion.Version.Number > 0)
			{
				newVersion = versionAddProxy.Versions.FirstOrDefault(v => v.Language.Equals(baseVersion.Language.CultureInfo) && v.VersionNumber.Equals(baseVersion.Version.Number));

				// the new version may not exist if we are using language fallback and adding a new version. If that's the case we should create a blank version, as that's what Sitecore does.
				if (newVersion != null)
					newVersion = new ProxyItemVersion(newVersion) { VersionNumber = newVersionNumber }; // creating a new proxyversion essentially clones the existing version
				else newVersion = new ProxyItemVersion(baseVersion.Language.CultureInfo, newVersionNumber);
			}
			else newVersion = new ProxyItemVersion(baseVersion.Language.CultureInfo, newVersionNumber);

			// inject the new version we created into the proxy item
			var newVersions = versionAddProxy.Versions.Concat(new[] { newVersion }).ToArray();
			versionAddProxy.Versions = newVersions;

			// flush to serialization data store
			SerializeItemIfIncluded(versionAddProxy, "Version Added");
		}

		public virtual void DeleteItem(ItemDefinition itemDefinition, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			// use cache or else item is already gone from base DP
			// (in the case of TpSync, it's still there because base.Delete hasn't removed it from serialized)
			var existingItem = GetSourceFromId(itemDefinition.ID, true);

			if (existingItem == null) return; // it was already gone or an item from a different data provider

			if (_targetDataStore.Remove(existingItem))
				_logger.DeletedItem(_targetDataStore.FriendlyName, existingItem);
		}

		public virtual void RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromIdIfIncluded(itemDefinition);

			if (sourceItem == null) return; // predicate excluded item

			// create a clone of the item to remove the version from
			var versionRemovingProxy = new ProxyItem(sourceItem);

			// exclude the removed version
			versionRemovingProxy.Versions = versionRemovingProxy.Versions.Where(v => !v.Language.Equals(version.Language.CultureInfo) || !v.VersionNumber.Equals(version.Version.Number));

			SerializeItemIfIncluded(versionRemovingProxy, "Version Removed");
		}

		public virtual void RemoveVersions(ItemDefinition itemDefinition, Language language, bool removeSharedData, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromIdIfIncluded(itemDefinition);

			if (sourceItem == null) return;

			// create a clone of the item to remove the versions from
			var versionRemovingProxy = new ProxyItem(sourceItem);

			// drop all versions in the language
			versionRemovingProxy.Versions = versionRemovingProxy.Versions.Where(v => !v.Language.Equals(language.CultureInfo));

			SerializeItemIfIncluded(versionRemovingProxy, "Versions Removed");
		}

		/*
		 *	TRANSPARENT SYNC
		 *	This part of the data provider handles reading from a serialization store and 'ghosting' that into Sitecore
		 *	As if they were database items.
		 * 
		*/
		public virtual IEnumerable<ID> GetChildIds(ItemDefinition itemDefinition, CallContext context)
		{
			if (DisableSerialization || DisableTransparentSync) return Enumerable.Empty<ID>();

			// expectation: do not return null, return empty enumerable for not included etc
			var parentItem = GetTargetFromId(itemDefinition.ID);

			if (parentItem == null) return Enumerable.Empty<ID>();

			return _targetDataStore.GetChildren(parentItem).Select(item => new ID(item.Id));
		}

		/// <summary>
		/// Gets additional children that should be added IN ADDITION TO the base database children.
		/// This is used to patch in TpSync root items that do not exist in the database under items that do exist in the database.
		/// </summary>
		public virtual IEnumerable<ID> GetAdditionalChildIds(ItemDefinition itemDefinition, CallContext context)
		{
			if (DisableSerialization || DisableTransparentSync) return Enumerable.Empty<ID>();

			EnsureRootsInitialized();

			if (!RootIds.ContainsKey(itemDefinition.ID.Guid)) return Enumerable.Empty<ID>();

			return RootIds[itemDefinition.ID.Guid];
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
				if (sharedField.BlobId.HasValue || sharedField.Value != null)
					fields.Add(new ID(sharedField.FieldId), sharedField.BlobId?.ToString() ?? sharedField.Value);
			}

			var version = item.Versions.FirstOrDefault(v => v.VersionNumber == versionUri.Version.Number && v.Language.Name == versionUri.Language.Name);

			if (version == null) return fields;

			foreach (var versionedField in version.Fields)
			{
				if (versionedField.BlobId.HasValue || versionedField.Value != null)
					fields.Add(new ID(versionedField.FieldId), versionedField.BlobId?.ToString() ?? versionedField.Value);
			}

			var unversionedFields = item.UnversionedFields.FirstOrDefault(language => language.Language.Name == versionUri.Language.Name);
			if (unversionedFields != null)
			{
				foreach (var unversionedField in unversionedFields.Fields)
				{
					fields.Add(new ID(unversionedField.FieldId), unversionedField.BlobId?.ToString() ?? unversionedField.Value);
				}
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

		/// <summary>
		/// Restoring items from the recycle bin does not invoke the data provider at all, so we have to attach to its event
		/// to cause restored items to be rewritten to disk if they are included.
		/// </summary>
		protected virtual void HandleItemRestored(RestoreItemCompletedEvent restoreItemCompletedEvent)
		{
			if (!restoreItemCompletedEvent.DatabaseName.Equals(Database.Name, StringComparison.Ordinal)) return;

			// we use a timer to delay the execution of our handler for a couple seconds.
			// at the time the handler is called, calling Database.GetItem(id) returns NULL,
			// or without cache an item with an orphan path. The delay allows Sitecore to catch up
			// with it.
			new Timer(state =>
			{
				var item = GetItemFromId(new ID(restoreItemCompletedEvent.ItemId), true);

				Assert.IsNotNull(item, "Item that was restored was null.");

				var iitem = new ItemData(item);

				SerializeItemIfIncluded(iitem, "Restored");
			}, null, 2000, Timeout.Infinite);
		}

		protected virtual bool SerializeItemIfIncluded(IItemData item, string triggerReason)
		{
			Assert.ArgumentNotNull(item, "item");

			if (!_predicate.Includes(item).IsIncluded) return false;

			_targetDataStore.Save(item);
			_logger.SavedItem(_targetDataStore.FriendlyName, item, triggerReason);

			return true;
		}

		protected virtual bool HasConsequentialChanges(ItemChanges changes)
		{
			// properties, e.g. template, etc are always consequential
			// NOTE: sometimes you can get spurious 'changes' where the old and new value are the same. We reject those.
			if (changes.HasPropertiesChanged && changes.Properties.Any(x => !x.Value.OriginalValue.Equals(x.Value.Value))) return true;

			foreach (FieldChange change in changes.FieldChanges)
			{
				// NOTE: we do not check for old and new value equality here
				// because during package installation Sitecore will set item fields using
				// identical old and new values in the changes - for fields that have never been set before.
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

		protected virtual IItemData GetSourceFromIdIfIncluded(ItemDefinition itemDefinition)
		{
			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromId(itemDefinition.ID);

			if (sourceItem == null) return null;
			if (!_predicate.Includes(sourceItem).IsIncluded) return null;

			return sourceItem;
		}

		protected virtual IItemData GetSourceFromId(ID id, bool useCache = false, bool allowTpSyncFallback = true)
		{
			var item = GetItemFromId(id, useCache);

			if (item == null)
			{
				if (allowTpSyncFallback && !DisableTransparentSync)
				{
					// note: if reliant on item data, e.g. for saves, this fallback will not have any updates applied
					// and is thus useless. Disallow fallback for that case but you need data from elsewhere.
					return _targetDataStore.GetById(id.Guid, Database.Name);
				}

				return null;
			}

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

			if (metadata == null) return;
			
			if(metadata.TemplateId == TemplateIDs.Template.Guid || metadata.TemplateId == TemplateIDs.TemplateField.Guid || metadata.Path.EndsWith("__Standard Values", StringComparison.OrdinalIgnoreCase))
				Database.Engines.TemplateEngine.Reset();

			if (_syncConfiguration.UpdateLinkDatabase || _syncConfiguration.UpdateSearchIndex)
			{
				var item = GetItemFromId(new ID(metadata.Id), true);

				if (item == null) return;

				if (_syncConfiguration.UpdateLinkDatabase)
				{
					Globals.LinkDatabase.UpdateReferences(item);
				}

				if (_syncConfiguration.UpdateSearchIndex)
				{
					foreach (var index in ContentSearchManager.Indexes)
					{
						IndexCustodian.UpdateItem(index, new SitecoreItemUniqueId(item.Uri));
					}
				}
			}
		}

		/// <summary>
		/// Initializes the root items into a lookup so that TpSync can inject roots
		/// under Sitecore items that are in the database that are TpSynced
		/// </summary>
		protected virtual void EnsureRootsInitialized()
		{
			if (RootIds != null) return;

			lock (_rootIdInitLock)
			{
				if (RootIds != null) return;

				var rootItems = _rootPathResolver.GetRootSerializedItems()
					.Where(root => root.DatabaseName.Equals(Database.Name));

				var rootItemLookup = new Dictionary<Guid, IReadOnlyList<ID>>();

				foreach (var root in rootItems)
				{
					var items = rootItemLookup.ContainsKey(root.ParentId)
						? new List<ID>(rootItemLookup[root.ParentId])
						: new List<ID>();

					items.Add(new ID(root.Id));

					rootItemLookup[root.ParentId] = items.AsReadOnly();
				}

				RootIds = new ReadOnlyDictionary<Guid, IReadOnlyList<ID>>(rootItemLookup);
			}
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
				targetAsDisposable?.Dispose();
			}
		}
	}
}
