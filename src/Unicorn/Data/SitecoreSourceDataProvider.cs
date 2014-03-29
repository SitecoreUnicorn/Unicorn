using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using System.Collections.Generic;
using Unicorn.ControlPanel;

namespace Unicorn.Data
{
	/// <summary>
	/// Acquires source data from Sitecore. This is just fine 99.9% of the time :)
	/// </summary>
	public class SitecoreSourceDataProvider : ISourceDataProvider, IDocumentable
	{
		public void ResetTemplateEngine()
		{
			foreach (Database current in Factory.GetDatabases())
			{
				current.Engines.TemplateEngine.Reset();
			}
		}

		public ISourceItem GetItemById(string database, ID id)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(id, "id");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			var dbItem = db.GetItem(id);

			if (dbItem == null) return null;

			return new SitecoreSourceItem(dbItem);
		}
		
		public ISourceItem GetItemByPath(string database, string path)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(path, "path");

			Database db = GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			var dbItem = db.GetItem(path);

			if (dbItem == null) return null;

			return new SitecoreSourceItem(dbItem);
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
