using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.SqlServer;
using Sitecore.Globalization;
using Unicorn.Dependencies;

namespace Unicorn
{
	public class UnicornSqlServerDataProvider : SqlServerDataProvider
	{
		private readonly UnicornDataProvider[] _unicornDataProviders;

		public UnicornSqlServerDataProvider(string connectionString) : this(connectionString, Registry.Default.Resolve<UnicornDataProvider>())
		{
		}
		
		public UnicornSqlServerDataProvider(string connectionString, params UnicornDataProvider[] unicornDataProvider) : base(connectionString)
		{
			_unicornDataProviders = unicornDataProvider;
			foreach(var provider in _unicornDataProviders)
				provider.DataProvider = this;
		}

		public override bool SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
		{
			if (!base.SaveItem(itemDefinition, changes, context)) return false;

			foreach (var provider in _unicornDataProviders)
				provider.SaveItem(itemDefinition, changes, context);

			return true;
		}

		public override bool MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
		{
			if (!base.MoveItem(itemDefinition, destination, context)) return false;

			foreach (var provider in _unicornDataProviders)
				provider.MoveItem(itemDefinition, destination, context);

			return true;
		}

// ReSharper disable once InconsistentNaming
		public override bool CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyID, CallContext context)
		{
			if (!base.CopyItem(source, destination, copyName, copyID, context)) return false;

			foreach (var provider in _unicornDataProviders)
				provider.CopyItem(source, destination, copyName, copyID, context);

			return true;
		}

		public override int AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
		{
			var baseVersionResult = base.AddVersion(itemDefinition, baseVersion, context);

			if (baseVersionResult < 1) return baseVersionResult; // no version created for some reason

			foreach (var provider in _unicornDataProviders)
				provider.AddVersion(itemDefinition, baseVersion, context);

			return baseVersionResult;
		}

		public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
		{
			if (!base.DeleteItem(itemDefinition, context)) return false;

			foreach (var provider in _unicornDataProviders)
				provider.DeleteItem(itemDefinition, context);

			return true;
		}

		public override bool RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
		{
			if (!base.RemoveVersion(itemDefinition, version, context)) return false;

			foreach(var provider in _unicornDataProviders)
				provider.RemoveVersion(itemDefinition, version, context);

			return true;
		}

		public override bool RemoveVersions(ItemDefinition itemDefinition, Language language, bool removeSharedData, CallContext context)
		{
			if (!base.RemoveVersions(itemDefinition, language, removeSharedData, context)) return false;

			foreach (var provider in _unicornDataProviders)
				provider.RemoveVersions(itemDefinition, language, removeSharedData, context);

			return true;
		}
	}
}
