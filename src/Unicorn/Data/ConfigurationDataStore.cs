using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Rainbow;
using Rainbow.Model;
using Rainbow.Storage;

namespace Unicorn.Data
{
	/// <summary>
	/// Facade that enables mapping arbitrary IDataStore implementations onto ISourceDataStore/ITargetDataStore,
	/// so that Unicorn can disambiguate which dependency it's after.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class ConfigurationDataStore : ISourceDataStore, ITargetDataStore
	{
		private readonly Lazy<IDataStore> _innerDataStore;

		public ConfigurationDataStore(Lazy<IDataStore> innerDataStore)
		{
			_innerDataStore = innerDataStore;
		}

		public IDataStore InnerDataStore => _innerDataStore.Value;

		public void Save(IItemData item)
		{
			_innerDataStore.Value.Save(item);
		}

		public void MoveOrRenameItem(IItemData itemWithFinalPath, string oldPath)
		{
			_innerDataStore.Value.MoveOrRenameItem(itemWithFinalPath, oldPath);
		}

		public IEnumerable<IItemData> GetByPath(string path, string database)
		{
			return _innerDataStore.Value.GetByPath(path, database);
		}

		public IItemData GetByPathAndId(string path, Guid id, string database)
		{
			return _innerDataStore.Value.GetByPathAndId(path, id, database);
		}

		public IItemData GetById(Guid id, string database)
		{
			return _innerDataStore.Value.GetById(id, database);
		}

		public IEnumerable<IItemMetadata> GetMetadataByTemplateId(Guid templateId, string database)
		{
			return _innerDataStore.Value.GetMetadataByTemplateId(templateId, database);
		}

		public IEnumerable<IItemData> GetChildren(IItemData parentItem)
		{
			return _innerDataStore.Value.GetChildren(parentItem);
		}

		public void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			_innerDataStore.Value.CheckConsistency(database, fixErrors, logMessageReceiver);
		}

		public void ResetTemplateEngine()
		{
			_innerDataStore.Value.ResetTemplateEngine();
		}

		public bool Remove(IItemData item)
		{
			return _innerDataStore.Value.Remove(item);
		}

		public void RegisterForChanges(Action<IItemMetadata, string> actionOnChange)
		{
			_innerDataStore.Value.RegisterForChanges(actionOnChange);
		}

		public void Clear()
		{
			_innerDataStore.Value.Clear();
		}

		public string FriendlyName => DocumentationUtility.GetFriendlyName(_innerDataStore.Value);
		public string Description => DocumentationUtility.GetDescription(_innerDataStore.Value);

		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return DocumentationUtility.GetConfigurationDetails(_innerDataStore.Value);
		}
	}
}
