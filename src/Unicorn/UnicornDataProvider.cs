using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Unicorn.Data;
using Unicorn.Serialization;
using Unicorn.Serialization.Sitecore;

namespace Unicorn
{
	public class UnicornDataProvider : WrappedDataProvider
	{
		private readonly ISerializationProvider _serializationProvider;

		public UnicornDataProvider(DataProvider realProvider, ISerializationProvider serializationProvider) : base(realProvider)
		{
			_serializationProvider = serializationProvider;
		}

		public override bool RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
		{
			if (!base.RemoveVersion(itemDefinition, version, context)) return false;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
			Assert.ArgumentNotNull(version, "version");

			var existingItem = GetExistingSerializedItem(itemDefinition.ID);

			Assert.IsNotNull(existingItem, "Existing item {0} did not exist in the serialization provider!", itemDefinition.ID);

			var syncVersion = existingItem.GetVersion(version.Language.Name, version.Version.Number);

			Assert.IsNotNull(syncVersion, "Version to remove {0}#{1} did not exist on {2}!", version.Language.Name, version.Version.Number, itemDefinition.ID);

			existingItem.RemoveVersion(syncVersion);

			SerializedDatabase.SaveItem(existingItem);

			return true;
		}

		public override bool RemoveVersions(ItemDefinition itemDefinition, Language language, bool removeSharedData, CallContext context)
		{
			if (!base.RemoveVersions(itemDefinition, language, removeSharedData, context)) return false;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
			Assert.ArgumentNotNull(language, "language");

			var existingItem = GetExistingSerializedItem(itemDefinition.ID);

			Assert.IsNotNull(existingItem, "Existing item {0} did not exist in the serialization store!", itemDefinition.ID);

			existingItem.RemoveVersions(language.Name);

			_serializationProvider.SerializeItem(existingItem);

			return true;
		}

		public override bool SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
		{
			if (!base.SaveItem(itemDefinition, changes, context)) return false;

			Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
			Assert.ArgumentNotNull(changes, "changes");

			var sourceItem = new SitecoreSourceItem(changes.Item);

			if (changes.Renamed)
			{
				string oldName = changes.Properties["name"].OriginalValue.ToString();
				_serializationProvider.RenameSerializedItem(sourceItem, oldName);
			}
			else 
				_serializationProvider.SerializeItem(sourceItem);

			return true;
		}

		private ISerializedItem GetExistingSerializedItem(ID id)
		{
			Assert.ArgumentNotNullOrEmpty(id, "id");

			var item = Database.GetItem(id);

			if (item == null) return null;

			var reference = _serializationProvider.GetReference(item.Paths.FullPath, Database.Name);

			return _serializationProvider.GetItem(reference);
		}
	}
}
