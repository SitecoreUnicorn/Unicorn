using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.SqlServer;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Unicorn.Configuration;

namespace Unicorn.Data.DataProvider
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

		protected ReadOnlyCollection<UnicornDataProvider> UnicornDataProviders => _unicornDataProviders.AsReadOnly();

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
			dataProvider.ParentDataProvider = this;
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

		public override bool ChangeTemplate(ItemDefinition itemDefinition, TemplateChangeList changeList, CallContext context)
		{
			if (!base.ChangeTemplate(itemDefinition, changeList, context)) return false;

			foreach (var provider in _unicornDataProviders)
				provider.ChangeTemplate(itemDefinition, changeList, context);

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
			bool unicornChildrenAreAuthoritative = false;

			foreach (var provider in UnicornDataProviders)
			{
				IEnumerable<ID> childrenResult;
				var providerResult = provider.GetChildIds(itemDefinition, context, out childrenResult);

				foreach (var result in childrenResult)
				{
					if (!results.Contains(result)) results.Add(result);
				}

				if (providerResult) unicornChildrenAreAuthoritative = true;
			}

			if (results.Count == 0 && !unicornChildrenAreAuthoritative)
			{
				// get database children
				var baseIds = base.GetChildIDs(itemDefinition, context);

				// get additional children from Unicorn providers
				// e.g. for TpSync if the root item of a tree is not in the database
				// this allows us to inject that root as a child of the DB item
				foreach (var provider in UnicornDataProviders)
				{
					var providerResult = provider.GetAdditionalChildIds(itemDefinition, context);
					foreach (var result in providerResult)
					{
						// if the db children returned null, we need to make a new list (because we DO have children)
						if(baseIds == null) baseIds = new IDList();

						if (!baseIds.Contains(result)) baseIds.Add(result);
					}
				}

				return baseIds;
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
			if (baseResult != null)
			{
				foreach (ID result in baseResult)
				{
					if (!results.Contains(result)) results.Add(result);
				}
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

		public override bool BlobStreamExists(Guid blobId, CallContext context)
		{
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.BlobStreamExists(blobId, context);
				if (providerResult) return true;
			}

			return base.BlobStreamExists(blobId, context);
		}

		public override Stream GetBlobStream(Guid blobId, CallContext context)
		{
			// of note: we do not need SetBlobStream() to get overridden because we write blobs in SaveItem()
			foreach (var provider in UnicornDataProviders)
			{
				var providerResult = provider.GetBlobStream(blobId, context);
				if (providerResult != null) return providerResult;
			}

			return base.GetBlobStream(blobId, context);
		}

		protected bool DisableFastQueryLogging = Settings.GetBoolSetting("Unicorn.DisableFastQueryWarning", false);
		protected override IDList QueryFast(string query, CallContext context)
		{
			if (!DisableFastQueryLogging && UnicornDataProviders.Any(provider => !provider.DisableTransparentSync))
			{
				Log.Warn("[Unicorn] A Fast Query was performed and Unicorn had one or more configurations enabled that used Transparent Sync. Fast Query is not supported with Transparent Sync. Either stop using Fast Query (it's generally regarded as a bad idea in almost every circumstance), or disable Transparent Sync for all configurations.", this);
				Log.Warn("[Unicorn] The Fast Query was: " + query, this);
				Log.Warn("[Unicorn] The call stack that made the Fast Query was: " + new StackTrace(), this);
			}

			return base.QueryFast(query, context);
		}
	}
}
