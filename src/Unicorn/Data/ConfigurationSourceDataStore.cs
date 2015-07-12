using System;
using System.Collections.Generic;
using Rainbow.Model;
using Rainbow.Storage;
using Unicorn.ControlPanel;

namespace Unicorn.Data
{
	/// <summary>
	/// Facade that enables mapping arbitrary IDataStore implementations onto ISourceDataStore/ITargetDataStore,
	/// so that Unicorn can disambiguate which dependency it's after.
	/// </summary>
	public class ConfigurationDataStore : ISourceDataStore, ITargetDataStore, IDocumentable
	{
		private readonly Lazy<IDataStore> _innerDataStore;

		public ConfigurationDataStore(Lazy<IDataStore> innerDataStore)
		{
			_innerDataStore = innerDataStore;
		}

		public IEnumerable<string> GetDatabaseNames()
		{
			return _innerDataStore.Value.GetDatabaseNames();
		}

		public void Save(IItemData item)
		{
			_innerDataStore.Value.Save(item);
		}

		public IItemData GetById(Guid itemId, string database)
		{
			return _innerDataStore.Value.GetById(itemId, database);
		}

		public IEnumerable<IItemData> GetByPath(string path, string database)
		{
			return _innerDataStore.Value.GetByPath(path, database);
		}

		public IEnumerable<IItemData> GetByTemplate(Guid templateId, string database)
		{
			return _innerDataStore.Value.GetByTemplate(templateId, database);
		}

		public IEnumerable<IItemData> GetChildren(Guid parentId, string database)
		{
			return _innerDataStore.Value.GetChildren(parentId, database);
		}

		public IEnumerable<IItemData> GetDescendants(Guid parentId, string database)
		{
			return _innerDataStore.Value.GetDescendants(parentId, database);
		}

		public void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver)
		{
			_innerDataStore.Value.CheckConsistency(database, fixErrors, logMessageReceiver);
		}

		public void ResetTemplateEngine()
		{
			_innerDataStore.Value.ResetTemplateEngine();
		}

		public bool Remove(Guid itemId, string database)
		{
			return _innerDataStore.Value.Remove(itemId, database);
		}

		public string FriendlyName { get { return _innerDataStore.Value.GetType().Name; } }
		public string Description { get { return _innerDataStore.Value.GetType().AssemblyQualifiedName; } }
		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return null;
		}
	}
}
