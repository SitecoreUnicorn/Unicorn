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

		public string FriendlyName { get { return _innerDataStore.Value.GetType().Name; } }
		public string Description { get { return _innerDataStore.Value.GetType().AssemblyQualifiedName; } }
		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return null;
		}
	}
}
