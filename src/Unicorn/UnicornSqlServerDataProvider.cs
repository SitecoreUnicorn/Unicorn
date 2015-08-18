using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.SqlServer;
using Sitecore.Globalization;
using System.Collections.ObjectModel;
using Sitecore.Collections;
using Unicorn.Configuration;

namespace Unicorn
{
	/// <summary>
	/// This is a Sitecore data provider that effectively provides reliable eventing services to one or more UnicornDataProviders,
	/// which map to an individual configuration of Unicorn. This provider is a facade around the standard SqlServerDataProvider, so you
	/// REPLACE the existing data provider for databases that use Unicorn.
	/// 
	/// To apply your own set of Unicorn data providers, inherit from this class and have your constructor call base(connectionString, null)
	/// Then in your constructor call AddUnicornDataProvider() to add your provider(s) as needed.
	/// 
	/// If you're not using SQL server, you can implement an analogous provider to this one but inherit from your data provider type.
	/// </summary>
	public class UnicornSqlServerDataProvider : SqlServerDataProvider
	{
		private readonly List<UnicornDataProvider> _unicornDataProviders = new List<UnicornDataProvider>();
		protected ReadOnlyCollection<UnicornDataProvider> UnicornDataProviders
		{
			get { return _unicornDataProviders.AsReadOnly(); }
		}

		public UnicornSqlServerDataProvider(string connectionString)
			: this(connectionString, UnicornConfigurationManager.Configurations.Select(x => x.Resolve<UnicornDataProvider>()).ToArray())
		{
		}

		protected UnicornSqlServerDataProvider(string connectionString, params UnicornDataProvider[] unicornDataProviders)
			: base(connectionString)
		{
			if (unicornDataProviders == null) return; // you can pass null for this from derived classes
			// for times when you want to use AddUnicornDataProvider() to construct your providers from a method
			// instead of inlined in the :base() call.

			foreach (var provider in unicornDataProviders)
				AddUnicornDataProvider(provider);
		}

		protected void AddUnicornDataProvider(UnicornDataProvider dataProvider)
		{
			dataProvider.DataProvider = this;
			_unicornDataProviders.Add(dataProvider);
		}

		public override bool CreateItem(ID itemId, string itemName, ID templateId, ItemDefinition parent, CallContext context)
		{
			if (!base.CreateItem(itemId, itemName, templateId, parent, context)) return false;

			var newItem = GetItemDefinition(itemId, context);

			foreach (var provider in _unicornDataProviders)
				provider.CreateItem(newItem, templateId, parent, context);

			return true;
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

			foreach (var provider in _unicornDataProviders)
				provider.RemoveVersion(itemDefinition, version, context);

			return true;
		}

		public override bool RemoveVersions(ItemDefinition itemDefinition, Language language, bool removeSharedData, CallContext context)
		{
			if (!base.RemoveVersions(itemDefinition, language, removeSharedData, context)) return false;

			foreach (var provider in UnicornDataProviders)
				provider.RemoveVersions(itemDefinition, language, removeSharedData, context);

			return true;
		}

		public override IDList GetChildIDs(ItemDefinition itemDefinition, CallContext context)
		{
			var results = new HashSet<ID>();
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.GetChildIds(itemDefinition, context);
				foreach (var result in providerResult)
				{
					if (!results.Contains(result)) results.Add(result);
				}
			}

			var baseResult = base.GetChildIDs(itemDefinition, context);
			foreach (ID result in baseResult)
			{
				if (!results.Contains(result)) results.Add(result);
			}

			return IDList.Build(results.ToArray());
		}

		public override ItemDefinition GetItemDefinition(ID itemId, CallContext context)
		{
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.GetItemDefinition(itemId, context);
				if (providerResult != null) return providerResult;
			}

			return base.GetItemDefinition(itemId, context);
		}

		public override FieldList GetItemFields(ItemDefinition itemDefinition, VersionUri versionUri, CallContext context)
		{
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.GetItemFields(itemDefinition, versionUri, context);
				if (providerResult != null) return providerResult;
			}

			return base.GetItemFields(itemDefinition, versionUri, context);
		}

		public override VersionUriList GetItemVersions(ItemDefinition itemDefinition, CallContext context)
		{
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.GetItemVersions(itemDefinition, context);
				if (providerResult != null) return providerResult;
			}

			return base.GetItemVersions(itemDefinition, context);
		}

		public override ID GetParentID(ItemDefinition itemDefinition, CallContext context)
		{
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.GetParentId(itemDefinition, context);
				if (providerResult != (ID)null) return providerResult;
			}

			return base.GetParentID(itemDefinition, context);
		}

		public override ID ResolvePath(string itemPath, CallContext context)
		{
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.ResolvePath(itemPath, context);
				if (providerResult != (ID)null) return providerResult;
			}

			return base.ResolvePath(itemPath, context);
		}

		public override IdCollection GetTemplateItemIds(CallContext context)
		{
			var results = new HashSet<ID>();
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.GetTemplateItemIds(context);
				foreach (var result in providerResult)
				{
					if (!results.Contains(result)) results.Add(result);
				}
			}

			var baseResult = base.GetTemplateItemIds(context);
			foreach (ID result in baseResult)
			{
				if (!results.Contains(result)) results.Add(result);
			}

			var collection = new IdCollection();
			collection.Add(results.ToArray());

			return collection;
		}

		public override bool HasChildren(ItemDefinition itemDefinition, CallContext context)
		{
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.HasChildren(itemDefinition, context);
				if (providerResult != null) return providerResult.Value;
			}

			return base.HasChildren(itemDefinition, context);
		}
	}
}
