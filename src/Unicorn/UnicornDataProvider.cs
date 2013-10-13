using System.Linq;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Unicorn.Data;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn
{
	public class UnicornDataProvider : WrappedDataProvider
	{
		private readonly ISerializationProvider _serializationProvider;
		private readonly IPredicate _predicate;

		public UnicornDataProvider(DataProvider realProvider, ISerializationProvider serializationProvider, IPredicate predicate)
			: base(realProvider)
		{
			_predicate = predicate;
			_serializationProvider = serializationProvider;
		}

		/// <summary>
		/// Disables all serialization handling if true. Used during serialization load tasks.
		/// </summary>
		public static bool DisableSerialization { get; set; }

		public override bool CreateItem(ID itemId, string itemName, ID templateId, ItemDefinition parent, CallContext context)
		{
			if (!base.CreateItem(itemId, itemName, templateId, parent, context)) return false;
			if (DisableSerialization) return true;

			// TODO: do we need to handle this? (if so we need a way to create an ISerializedItem from scratch...)

			return true;
		}

		public override bool SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
		{
			if (!base.SaveItem(itemDefinition, changes, context)) return false;
			if (DisableSerialization) return true;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
			Assert.ArgumentNotNull(changes, "changes");

			var sourceItem = new SitecoreSourceItem(changes.Item);

			if (!_predicate.Includes(sourceItem).IsIncluded) return true;

			if (changes.Renamed)
			{
				string oldName = changes.Properties["name"].OriginalValue.ToString();
				_serializationProvider.RenameSerializedItem(sourceItem, oldName);
			}
			else
				_serializationProvider.SerializeItem(sourceItem);

			return true;
		}

		public override bool MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
		{
			if (!base.MoveItem(itemDefinition, destination, context)) return false;
			if (DisableSerialization) return true;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromDefinition(destination);
			var destinationItem = GetSourceFromDefinition(destination);

			if (!_predicate.Includes(destinationItem).IsIncluded) // if the destination we are moving to is NOT included for serialization, we delete the existing item
			{
				var existingItem = GetExistingSerializedItem(sourceItem.Id);

				if (existingItem != null) _serializationProvider.DeleteSerializedItem(existingItem);

				return true;
			}

			_serializationProvider.MoveSerializedItem(sourceItem, destination.ID);

			return true;
		}

		public override bool CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyID, CallContext context)
		{
			if (!base.CopyItem(source, destination, copyName, copyID, context)) return false;
			if (DisableSerialization) return true;

			var destinationItem = GetSourceFromDefinition(destination);

			if (!_predicate.Includes(destinationItem).IsIncluded) return true; // destination parent is not in a path that we are serializing, so skip out

			var sourceItem = GetSourceFromDefinition(source);

			_serializationProvider.CopySerializedItem(sourceItem, destinationItem);

			return true;
		}

		public override int AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
		{
			var baseVersionResult = base.AddVersion(itemDefinition, baseVersion, context);

			if (baseVersionResult < 1) return baseVersionResult; // no version created for some reason
			if (DisableSerialization) return baseVersionResult;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			SerializeItemIfIncluded(itemDefinition);

			return baseVersionResult;
		}

		public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
		{
			if (!base.DeleteItem(itemDefinition, context)) return false;
			if (DisableSerialization) return true;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var existingItem = GetExistingSerializedItem(itemDefinition.ID);

			if (existingItem == null) return true; // it was already gone or an item from a different data provider

			_serializationProvider.DeleteSerializedItem(existingItem);

			return true;
		}

		public override bool RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
		{
			if (!base.RemoveVersion(itemDefinition, version, context)) return false;
			if (DisableSerialization) return true;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			SerializeItemIfIncluded(itemDefinition);

			return true;
		}

		public override bool RemoveVersions(ItemDefinition itemDefinition, Language language, bool removeSharedData, CallContext context)
		{
			if (!base.RemoveVersions(itemDefinition, language, removeSharedData, context)) return false;
			if (DisableSerialization) return true;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
		
			SerializeItemIfIncluded(itemDefinition);

			return true;
		}

		protected virtual bool SerializeItemIfIncluded(ItemDefinition itemDefinition)
		{
			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

			var sourceItem = GetSourceFromDefinition(itemDefinition);

			if (!_predicate.Includes(sourceItem).IsIncluded) return false; // item was not included so we get out

			_serializationProvider.SerializeItem(sourceItem);

			return true;
		}

		protected virtual ISerializedItem GetExistingSerializedItem(ID id)
		{
			Assert.ArgumentNotNullOrEmpty(id, "id");

			var item = Database.GetItem(id);

			if (item == null) return null;

			var reference = _serializationProvider.GetReference(item.Paths.FullPath, Database.Name);

			return _serializationProvider.GetItem(reference);
		}

		protected virtual ISourceItem GetSourceFromDefinition(ItemDefinition definition)
		{
			return new SitecoreSourceItem(Database.GetItem(definition.ID));
		}
	}
}
