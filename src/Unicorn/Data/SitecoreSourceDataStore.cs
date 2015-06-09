using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using Unicorn.ControlPanel;

namespace Unicorn.Data
{
	/// <summary>
	/// Acquires source data from Sitecore. This is just fine 99.9% of the time :)
	/// </summary>
	public class SitecoreSourceDataStore : ISourceDataStore, IDocumentable
	{
		public void Recycle(ISerializableItem item)
		{
			Assert.ArgumentNotNull(item, "item");

			var database = GetDatabase(item.DatabaseName);
			var itemId = new ID(item.Id);
			var sitecoreItem = database.GetItem(itemId);

			sitecoreItem.Recycle();

			if (EventDisabler.IsActive)
			{
				database.Caches.ItemCache.RemoveItem(itemId);
				database.Caches.DataCache.RemoveItemInformation(itemId);
			}

			if (database.Engines.TemplateEngine.IsTemplatePart(sitecoreItem))
				database.Engines.TemplateEngine.Reset();
		}

		public void ResetTemplateEngine()
		{
			foreach (Database current in Factory.GetDatabases())
			{
				current.Engines.TemplateEngine.Reset();
			}
		}

		public ISerializableItem GetById(string database, Guid id)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			var dbItem = db.GetItem(new ID(id));

			if (dbItem == null) return null;

			return new SerializableItem(dbItem);
		}

		public ISerializableItem GetByPath(string database, string path)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(path, "path");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			var dbItem = db.GetItem(path);

			if (dbItem == null) return null;

			return new SerializableItem(dbItem);
		}

		public ISerializableItem[] GetChildren(ISerializableItem parent)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var db = GetDatabase(parent.DatabaseName);

			Assert.IsNotNull(db, "Database of item was null! Security issue?");

			var item = db.GetItem(new ID(parent.Id));

			return item.Children.Select(x => (ISerializableItem)new SerializableItem(x)).ToArray();
		}

		public void DeserializationComplete(string databaseName)
		{
			Assert.ArgumentNotNullOrEmpty(databaseName, "databaseName");

			EventManager.RaiseEvent(new SerializationFinishedEvent());
			Database database = Factory.GetDatabase(databaseName, false);
			if (database != null)
			{
				database.RemoteEvents.Queue.QueueEvent(new SerializationFinishedEvent());
			}
		}

		private Database GetDatabase(string databaseName)
		{
			return Factory.GetDatabase(databaseName);
		}

		public string FriendlyName
		{
			get { return "Sitecore Source Data Provider"; }
		}

		public string Description
		{
			get { return "Retrieves source items from Sitecore databases."; }
		}

		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return null;
		}
	}
}
