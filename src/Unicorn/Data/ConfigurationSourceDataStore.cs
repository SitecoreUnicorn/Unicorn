using System;
using System.Collections.Generic;
using Rainbow.Model;
using Rainbow.Storage;

namespace Unicorn.Data
{
	/// <summary>
	/// Facade that enables mapping arbitrary IDataStore implementations onto ISourceDataStore/ITargetDataStore,
	/// so that Unicorn can disambiguate which dependency it's after.
	/// </summary>
	public class ConfigurationDataStore : ISourceDataStore, ITargetDataStore
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

		public void Save(ISerializableItem item)
		{
			_innerDataStore.Value.Save(item);
		}

		public ISerializableItem GetById(Guid itemId, string database)
		{
			return _innerDataStore.Value.GetById(itemId, database);
		}

		public IEnumerable<ISerializableItem> GetByPath(string path, string database)
		{
			return _innerDataStore.Value.GetByPath(path, database);
		}

		public IEnumerable<ISerializableItem> GetByTemplate(Guid templateId, string database)
		{
			return _innerDataStore.Value.GetByTemplate(templateId, database);
		}

		public IEnumerable<ISerializableItem> GetChildren(Guid parentId, string database)
		{
			return _innerDataStore.Value.GetChildren(parentId, database);
		}

		public IEnumerable<ISerializableItem> GetDescendants(Guid parentId, string database)
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
	}
}
