using System.Linq;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.SqlServer;
using Sitecore.Globalization;
using Unicorn.Predicates;
using Unicorn.Serialization.Sitecore;

namespace Unicorn
{
	public class UnicornSqlServerDataProvider : SqlServerDataProvider
	{
		private readonly UnicornDataProvider _unicornDataProvider;

		public UnicornSqlServerDataProvider(string connectionString) : this(connectionString, new UnicornDataProvider(new SitecoreSerializationProvider(), new SerializationPresetPredicate()))
		{
		}
		
		public UnicornSqlServerDataProvider(string connectionString, UnicornDataProvider unicornDataProvider) : base(connectionString)
		{
			_unicornDataProvider = unicornDataProvider;
			_unicornDataProvider.DataProvider = this;
		}

		public override bool CreateItem(ID itemId, string itemName, ID templateId, ItemDefinition parent, CallContext context)
		{
			if (!base.CreateItem(itemId, itemName, templateId, parent, context)) return false;

			_unicornDataProvider.CreateItem(itemId, itemName, templateId, parent, context);

			return true;
		}

		public override bool SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
		{
			if (!base.SaveItem(itemDefinition, changes, context)) return false;

			_unicornDataProvider.SaveItem(itemDefinition, changes, context);

			return true;
		}

		public override bool MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
		{
			if (!base.MoveItem(itemDefinition, destination, context)) return false;

			_unicornDataProvider.MoveItem(itemDefinition, destination, context);

			return true;
		}

		public override bool CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyID, CallContext context)
		{
			if (!base.CopyItem(source, destination, copyName, copyID, context)) return false;

			_unicornDataProvider.CopyItem(source, destination, copyName, copyID, context);

			return true;
		}

		public override int AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
		{
			var baseVersionResult = base.AddVersion(itemDefinition, baseVersion, context);

			if (baseVersionResult < 1) return baseVersionResult; // no version created for some reason

			_unicornDataProvider.AddVersion(itemDefinition, baseVersion, context);

			return baseVersionResult;
		}

		public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
		{
			if (!base.DeleteItem(itemDefinition, context)) return false;

			_unicornDataProvider.DeleteItem(itemDefinition, context);

			return true;
		}

		public override bool RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
		{
			if (!base.RemoveVersion(itemDefinition, version, context)) return false;

			_unicornDataProvider.RemoveVersion(itemDefinition, version, context);

			return true;
		}

		public override bool RemoveVersions(ItemDefinition itemDefinition, Language language, bool removeSharedData, CallContext context)
		{
			if (!base.RemoveVersions(itemDefinition, language, removeSharedData, context)) return false;

			_unicornDataProvider.RemoveVersions(itemDefinition, language, removeSharedData, context);

			return true;
		}
	}
}
