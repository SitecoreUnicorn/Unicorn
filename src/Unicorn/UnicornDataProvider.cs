using System;
using System.Linq;
using Rainbow.Model;
using Rainbow.Predicates;
using Rainbow.Storage;
using Rainbow.Storage.Sc;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Unicorn.Data;
using Unicorn.Predicates;

namespace Unicorn
{
	/// <summary>
	/// This class provides event-handling services to Unicorn - reflecting actions onto the serialization provider via the predicate when
	/// changes occur to the source data.
	/// </summary>
	public class UnicornDataProvider
	{
		private readonly ITargetDataStore _targetDataStore;
		private readonly IPredicate _predicate;
		private readonly IFieldPredicate _fieldPredicate;
		private readonly IUnicornDataProviderLogger _logger;
		private static bool _disableSerialization;

		public UnicornDataProvider(ITargetDataStore targetDataStore, IPredicate predicate, IFieldPredicate fieldPredicate, IUnicornDataProviderLogger logger)
		{
			Assert.ArgumentNotNull(targetDataStore, "serializationProvider");
			Assert.ArgumentNotNull(predicate, "predicate");
			Assert.ArgumentNotNull(fieldPredicate, "fieldPredicate");
			Assert.ArgumentNotNull(logger, "logger");

			_logger = logger;
			_predicate = predicate;
			_fieldPredicate = fieldPredicate;
			_targetDataStore = targetDataStore;
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

		public DataProvider DataProvider { get; set; }
		protected Database Database { get { return DataProvider.Database; } }

		public void CreateItem(ItemDefinition newItem, ID templateId, ItemDefinition parent, CallContext context)
		{
			if (DisableSerialization) return;

			if (DisableSerialization) return;

			Assert.ArgumentNotNull(newItem, "itemDefinition");

			SerializeItemIfIncluded(newItem, "Created");
		}

		public void SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
			Assert.ArgumentNotNull(changes, "changes");

			var sourceItem = GetItemWithoutCache(changes.Item);

			if (!_predicate.Includes(sourceItem).IsIncluded) return;

			string oldName = changes.Renamed ? changes.Properties["name"].OriginalValue.ToString() : string.Empty;
			if (changes.Renamed && !oldName.Equals(sourceItem.Name, StringComparison.Ordinal)) // it's a rename, in which the name actually changed (template builder will cause 'renames' for the same name!!!)
			{
				_targetDataStore.Save(sourceItem);
				_logger.RenamedItem(_targetDataStore.GetType().Name, sourceItem, oldName);
			}
			else if (HasConsequentialChanges(changes)) // it's a simple update - but we reject it if only inconsequential fields (last updated, revision) were changed - again, template builder FTW
			{
				_targetDataStore.Save(sourceItem);
				_logger.SavedItem(_targetDataStore.GetType().Name, sourceItem, "Saved");
			}
		}



		public void MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromDefinition(itemDefinition); // we don't use cache here because we want the post-move path
			var destinationItem = GetSourceFromDefinition(destination);

			if (!_predicate.Includes(destinationItem).IsIncluded) // if the destination we are moving to is NOT included for serialization, we delete the existing item
			{
				var existingItem = GetExistingSerializedItem(sourceItem.Id);

				if (existingItem != null)
				{
					_targetDataStore.Remove(existingItem.Id, existingItem.DatabaseName);
					_logger.MovedItemToNonIncludedLocation(_targetDataStore.GetType().Name, existingItem);
				}

				return;
			}

			_targetDataStore.Save(sourceItem);
			_logger.MovedItem(_targetDataStore.GetType().Name, sourceItem, destinationItem);
		}

		public void CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyId, CallContext context)
		{
			if (DisableSerialization) return;

			// copying is easy - all we have to do is serialize the copyID. Copied children will all result in multiple calls to CopyItem so we don't even need to worry about them.
			var copiedItem = new SerializableItem(Database.GetItem(copyId));

			if (!_predicate.Includes(copiedItem).IsIncluded) return; // destination parent is not in a path that we are serializing, so skip out

			_targetDataStore.Save(copiedItem);
			_logger.CopiedItem(_targetDataStore.GetType().Name, () => GetSourceFromDefinition(source), copiedItem);
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

			var existingItem = GetExistingSerializedItem(itemDefinition.ID.Guid);

			if (existingItem == null) return; // it was already gone or an item from a different data provider

			_targetDataStore.Remove(existingItem.Id, existingItem.DatabaseName);
			_logger.DeletedItem(_targetDataStore.GetType().Name, existingItem);
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

		protected virtual bool SerializeItemIfIncluded(ItemDefinition itemDefinition, string triggerReason)
		{
			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromDefinition(itemDefinition);

			if (!_predicate.Includes(sourceItem).IsIncluded) return false; // item was not included so we get out

			_targetDataStore.Save(sourceItem);
			_logger.SavedItem(_targetDataStore.GetType().Name, sourceItem, triggerReason);

			return true;
		}

		protected virtual ISerializableItem GetExistingSerializedItem(Guid id)
		{
			var item = Database.GetItem(new ID(id));

			if (item == null) return null;

			return _targetDataStore.GetById(item.ID.Guid, item.Database.Name);
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
				if (!_fieldPredicate.Includes(change.FieldID.Guid).IsIncluded) continue;

				return true;
			}

			_logger.SaveRejectedAsInconsequential(_targetDataStore.GetType().Name, changes);

			return false;
		}

		protected ISerializableItem GetSourceFromDefinition(ItemDefinition definition)
		{
			return GetSourceFromDefinition(definition, false);
		}

		protected virtual ISerializableItem GetSourceFromDefinition(ItemDefinition definition, bool useCache)
		{
			if (!useCache) RemoveItemFromCache(definition.ID);
			return new SerializableItem(Database.GetItem(definition.ID));
		}

		/// <summary>
		/// When items are acquired from the data provider they can be stale in cache, which fouls up serializing them.
		/// Renames and template changes are particularly vulnerable to this.
		/// </summary>
		protected virtual ISerializableItem GetItemWithoutCache(Item item)
		{
			RemoveItemFromCache(item.ID);

			// reacquire the source item after cleaning the cache
			return new SerializableItem(item.Database.GetItem(item.ID, item.Language, item.Version));
		}

		protected virtual void RemoveItemFromCache(ID id)
		{
			// in some cases, such as template changes, we can have outdated information in the itemChanges.Item
			// for example invalid field IDs in language versions that are not the current item version. To mitigate this threat
			// to invalid serialized values, we first nuke the item from the cache before we serialize it to make sure we get the freshest data
			Database.Caches.ItemCache.RemoveItem(id);
			Database.Caches.DataCache.RemoveItemInformation(id);
		}
	}
}
