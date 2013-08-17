using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Templates;
using Sitecore.Eventing;
using Sitecore.Globalization;
using Sitecore.Workflows;

namespace Unicorn
{
	/// <summary>
	/// A data provider that "wraps" another data provider. Used to provide "mixin" data services for any number of underlying actual data providers.
	/// </summary>
	public class WrappedDataProvider : DataProvider
	{
		private readonly DataProvider _realProvider;

		public WrappedDataProvider(DataProvider realProvider)
		{
			_realProvider = realProvider;
		}

		public override bool AddToPublishQueue(ID itemID, string action, DateTime date, CallContext context)
		{
			return _realProvider.AddToPublishQueue(itemID, action, date, context);
		}

		public override int AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
		{
			return _realProvider.AddVersion(itemDefinition, baseVersion, context);
		}

		public override bool BlobStreamExists(Guid blobId, CallContext context)
		{
			return _realProvider.BlobStreamExists(blobId, context);
		}

		public override CacheOptions CacheOptions
		{
			get
			{
				return _realProvider.CacheOptions;
			}
		}

		public override bool ChangeFieldSharing(TemplateField fieldDefinition, TemplateFieldSharing sharing, CallContext context)
		{
			return _realProvider.ChangeFieldSharing(fieldDefinition, sharing, context);
		}

		public override bool ChangeTemplate(ItemDefinition itemDefinition, TemplateChangeList changes, CallContext context)
		{
			return _realProvider.ChangeTemplate(itemDefinition, changes, context);
		}

		public override bool CleanupDatabase(CallContext context)
		{
			return _realProvider.CleanupDatabase(context);
		}

		public override bool CleanupPublishQueue(DateTime to, CallContext context)
		{
			return _realProvider.CleanupPublishQueue(to, context);
		}

		public override bool CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyID, CallContext context)
		{
			return _realProvider.CopyItem(source, destination, copyName, copyID, context);
		}

		public override bool CreateItem(ID itemID, string itemName, ID templateID, ItemDefinition parent, CallContext context)
		{
			return _realProvider.CreateItem(itemID, itemName, templateID, parent, context);
		}

		public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
		{
			return _realProvider.DeleteItem(itemDefinition, context);
		}

		public override bool Equals(object obj)
		{
			return _realProvider.Equals(obj);
		}

		public override Stream GetBlobStream(Guid blobId, CallContext context)
		{
			return _realProvider.GetBlobStream(blobId, context);
		}

		public override IDList GetChildIDs(ItemDefinition itemDefinition, CallContext context)
		{
			return _realProvider.GetChildIDs(itemDefinition, context);
		}

		public override long GetDataSize(int minEntitySize, int maxEntitySize)
		{
			return _realProvider.GetDataSize(minEntitySize, maxEntitySize);
		}

		public override long GetDictionaryEntryCount()
		{
			return _realProvider.GetDictionaryEntryCount();
		}

		public override EventQueue GetEventQueue()
		{
			return _realProvider.GetEventQueue();
		}

		public override int GetHashCode()
		{
			return _realProvider.GetHashCode();
		}

		public override ItemDefinition GetItemDefinition(ID itemId, CallContext context)
		{
			return _realProvider.GetItemDefinition(itemId, context);
		}

		public override FieldList GetItemFields(ItemDefinition itemDefinition, VersionUri versionUri, CallContext context)
		{
			return _realProvider.GetItemFields(itemDefinition, versionUri, context);
		}

		public override DataUri[] GetItemsInWorkflowState(WorkflowInfo info, CallContext context)
		{
			return _realProvider.GetItemsInWorkflowState(info, context);
		}

		public override VersionUriList GetItemVersions(ItemDefinition itemDefinition, CallContext context)
		{
			return _realProvider.GetItemVersions(itemDefinition, context);
		}

		public override LanguageCollection GetLanguages(CallContext context)
		{
			return _realProvider.GetLanguages(context);
		}

		public override ID GetParentID(ItemDefinition itemDefinition, CallContext context)
		{
			return _realProvider.GetParentID(itemDefinition, context);
		}

		public override string GetProperty(string name, CallContext context)
		{
			return _realProvider.GetProperty(name, context);
		}

		public override List<string> GetPropertyKeys(string prefix, CallContext context)
		{
			return _realProvider.GetPropertyKeys(prefix, context);
		}

		public override IDList GetPublishQueue(DateTime from, DateTime to, CallContext context)
		{
			return _realProvider.GetPublishQueue(from, to, context);
		}

		public override ID GetRootID(CallContext context)
		{
			return _realProvider.GetRootID(context);
		}

		public override IdCollection GetTemplateItemIds(CallContext context)
		{
			return _realProvider.GetTemplateItemIds(context);
		}

		public override TemplateCollection GetTemplates(CallContext context)
		{
			return _realProvider.GetTemplates(context);
		}

		public override WorkflowInfo GetWorkflowInfo(ItemDefinition item, VersionUri version, CallContext context)
		{
			return _realProvider.GetWorkflowInfo(item, version, context);
		}

		public override bool HasChildren(ItemDefinition itemDefinition, CallContext context)
		{
			return _realProvider.HasChildren(itemDefinition, context);
		}

		public override bool MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
		{
			return _realProvider.MoveItem(itemDefinition, destination, context);
		}

		public override bool RemoveBlobStream(Guid blobId, CallContext context)
		{
			return _realProvider.RemoveBlobStream(blobId, context);
		}

		public override void RemoveLanguageData(Language language, CallContext context)
		{
			_realProvider.RemoveLanguageData(language, context);
		}

		public override bool RemoveProperty(string name, bool isPrefix, CallContext context)
		{
			return _realProvider.RemoveProperty(name, isPrefix, context);
		}

		public override bool RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
		{
			return _realProvider.RemoveVersion(itemDefinition, version, context);
		}

		public override bool RemoveVersions(ItemDefinition itemDefinition, Language language, bool removeSharedData, CallContext context)
		{
			return _realProvider.RemoveVersions(itemDefinition, language, removeSharedData, context);
		}

		public override bool RemoveVersions(ItemDefinition itemDefinition, Language language, CallContext context)
		{
			return _realProvider.RemoveVersions(itemDefinition, language, context);
		}

		public override void RenameLanguageData(string fromLanguage, string toLanguage, CallContext context)
		{
			_realProvider.RenameLanguageData(fromLanguage, toLanguage, context);
		}

		public override ID ResolvePath(string itemPath, CallContext context)
		{
			return _realProvider.ResolvePath(itemPath, context);
		}

		public override bool SaveItem(ItemDefinition itemDefinition, Sitecore.Data.Items.ItemChanges changes, CallContext context)
		{
			return _realProvider.SaveItem(itemDefinition, changes, context);
		}

		public override IDList SelectIDs(string query, CallContext context)
		{
			return _realProvider.SelectIDs(query, context);
		}

		public override ID SelectSingleID(string query, CallContext context)
		{
			return _realProvider.SelectSingleID(query, context);
		}

		public override bool SetBlobStream(Stream stream, Guid blobId, CallContext context)
		{
			return _realProvider.SetBlobStream(stream, blobId, context);
		}

		public override bool SetProperty(string name, string value, CallContext context)
		{
			return _realProvider.SetProperty(name, value, context);
		}

		public override bool SetWorkflowInfo(ItemDefinition item, VersionUri version, WorkflowInfo info, CallContext context)
		{
			return _realProvider.SetWorkflowInfo(item, version, info, context);
		}
	}
}
