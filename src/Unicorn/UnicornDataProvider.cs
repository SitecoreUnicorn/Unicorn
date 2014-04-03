using System;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Unicorn.Data;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn
{
	/// <summary>
	/// This class provides event-handling services to Unicorn - reflecting actions onto the serialization provider via the predicate when
	/// changes occur to the source data.
	/// </summary>
	public class UnicornDataProvider
	{
		private readonly ISerializationProvider _serializationProvider;
		private readonly IPredicate _predicate;
		private readonly IFieldPredicate _fieldPredicate;
		private readonly IUnicornDataProviderLogger _logger;
		private static bool _disableSerialization;

		public UnicornDataProvider(ISerializationProvider serializationProvider, IPredicate predicate, IFieldPredicate fieldPredicate, IUnicornDataProviderLogger logger)
		{
			Assert.ArgumentNotNull(serializationProvider, "serializationProvider");
			Assert.ArgumentNotNull(predicate, "predicate");
			Assert.ArgumentNotNull(fieldPredicate, "fieldPredicate");
			Assert.ArgumentNotNull(logger, "logger");

			_logger = logger;
			_predicate = predicate;
			_fieldPredicate = fieldPredicate;
			_serializationProvider = serializationProvider;
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
				_serializationProvider.RenameSerializedItem(sourceItem, oldName);
				_logger.RenamedItem(_serializationProvider.LogName, sourceItem, oldName);
			}
			else if (HasConsequentialChanges(changes)) // it's a simple update - but we reject it if only inconsequential fields (last updated, revision) were changed - again, template builder FTW
			{
				_serializationProvider.SerializeItem(sourceItem);
				_logger.SavedItem(_serializationProvider.LogName, sourceItem, "Saved");
			}
		}

		

		public void MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
		{
			if (DisableSerialization) return;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromDefinition(itemDefinition);
			var destinationItem = GetSourceFromDefinition(destination);

			if (!_predicate.Includes(destinationItem).IsIncluded) // if the destination we are moving to is NOT included for serialization, we delete the existing item
			{
				var existingItem = GetExistingSerializedItem(sourceItem.Id);

				if (existingItem != null)
				{
					existingItem.Delete();
					_logger.MovedItemToNonIncludedLocation(_serializationProvider.LogName, existingItem);
				}

				return;
			}

			_serializationProvider.MoveSerializedItem(sourceItem, destinationItem);
			_logger.MovedItem(_serializationProvider.LogName, sourceItem, destinationItem);
		}

		public void CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyId, CallContext context)
		{
			if (DisableSerialization) return;

			// copying is easy - all we have to do is serialize the copyID. Copied children will all result in multiple calls to CopyItem so we don't even need to worry about them.
			RemoveItemFromCache(copyId);
			var copiedItem = new SitecoreSourceItem(Database.GetItem(copyId));

			if (!_predicate.Includes(copiedItem).IsIncluded) return; // destination parent is not in a path that we are serializing, so skip out

			_serializationProvider.SerializeItem(copiedItem);
			_logger.CopiedItem(_serializationProvider.LogName, () => GetSourceFromDefinition(source), copiedItem);
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

			var existingItem = GetExistingSerializedItem(itemDefinition.ID);

			if (existingItem == null) return; // it was already gone or an item from a different data provider

			existingItem.Delete();
			_logger.DeletedItem(_serializationProvider.LogName, existingItem);
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

			_serializationProvider.SerializeItem(sourceItem);
			_logger.SavedItem(_serializationProvider.LogName, sourceItem, triggerReason);

			return true;
		}

		protected virtual ISerializedItem GetExistingSerializedItem(ID id)
		{
			Assert.ArgumentNotNullOrEmpty(id, "id");

			var item = Database.GetItem(id);

			if (item == null) return null;

			var reference = _serializationProvider.GetReference(new SitecoreSourceItem(item));

			if (reference == null) return null;

			return reference.GetItem();
		}

		protected virtual bool HasConsequentialChanges(ItemChanges changes)
		{
			// properties, e.g. template, etc are always consequential
			if (changes.HasPropertiesChanged) return true;

			foreach (FieldChange change in changes.FieldChanges)
			{
				if (change.OriginalValue == change.Value) continue;
				if (change.FieldID == FieldIDs.Revision) continue;
				if (change.FieldID == FieldIDs.Updated) continue;
				if (!_fieldPredicate.Includes(change.FieldID).IsIncluded) continue;

				return true;
			}

			_logger.SaveRejectedAsInconsequential(_serializationProvider.LogName, changes);

			return false;
		}
		
		protected virtual ISourceItem GetSourceFromDefinition(ItemDefinition definition)
		{
			RemoveItemFromCache(definition.ID);
			return new SitecoreSourceItem(Database.GetItem(definition.ID));
		}

		/// <summary>
		/// When items are acquired from the data provider they can be stale in cache, which fouls up serializing them.
		/// Renames and template changes are particularly vulnerable to this.
		/// </summary>
		protected virtual SitecoreSourceItem GetItemWithoutCache(Item item)
		{
			RemoveItemFromCache(item.ID);

			// reacquire the source item after cleaning the cache
			return new SitecoreSourceItem(item.Database.GetItem(item.ID, item.Language, item.Version));
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
