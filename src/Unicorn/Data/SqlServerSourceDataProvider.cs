using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;

namespace Unicorn.Data
{
	/// <summary>
	/// Experimental provider that executes a direct SQL query to get only the pertinent data for an ISourceItem.
	/// It's MUCH faster than using the Sitecore APIs - for a 2,800 item core db on a SSD this is ~110ms, whereas APIs are ~4-6 sec.
	/// </summary>
	public class SqlServerSourceDataProvider : ISourceDataProvider
	{
		readonly Dictionary<string, CachedDatabase> _databases = new Dictionary<string, CachedDatabase>();

		readonly object _syncLock = new object();

		public ISourceItem GetItemById(string database, ID id)
		{
			EnsureDatabase(database);

			return _databases[database].GetItemById(id);
		}

		public ISourceItem GetItemByPath(string database, string path)
		{
			EnsureDatabase(database);

			return _databases[database].GetItemByPath(path);
		}

		public void ResetTemplateEngine()
		{
			new SitecoreSourceDataProvider().ResetTemplateEngine();
		}

		public void DeserializationComplete(string databaseName)
		{
			new SitecoreSourceDataProvider().DeserializationComplete(databaseName);
		}

		private void EnsureDatabase(string databaseName)
		{
			if (_databases.ContainsKey(databaseName)) return;

			lock (_syncLock)
			{
				if (_databases.ContainsKey(databaseName)) return;

				var db = new CachedDatabase(databaseName);

				_databases[databaseName] = db;
			}
		}

		private class CachedDatabase
		{
			private readonly string _databaseName;
			private readonly Dictionary<ID, CachedItem> _itemsById = new Dictionary<ID, CachedItem>();
			private readonly Dictionary<ID, IList<ID>> _childIndex = new Dictionary<ID, IList<ID>>();
			private readonly Dictionary<ID, string> _paths = new Dictionary<ID, string>();

			public CachedDatabase(string databaseName)
			{
				_databaseName = databaseName;

				LoadData();
			}

			private void LoadData()
			{
				var connectionString = ConfigurationManager.ConnectionStrings[Factory.GetDatabase(_databaseName).ConnectionStringName].ConnectionString;
				const string itemSql = @"SELECT i.Name, i.ID, i.ParentID, i.TemplateID, t.Name AS TemplateName
										FROM Items i
										INNER JOIN Items t ON i.TemplateID = t.ID";

				var fieldsSql = string.Format(@"SELECT ItemID, Language, Version, FieldId, Value
												FROM VersionedFields
												WHERE FieldId IN('{0}', '{1}')", FieldIDs.Updated, FieldIDs.Revision);

				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					using (var itemCommand = new SqlCommand(itemSql, connection))
					{
						using (var reader = itemCommand.ExecuteReader())
						{
							while (reader.Read())
							{
								var itemId = new ID(reader.GetGuid(1));

								CachedItem item;
								if (!_itemsById.TryGetValue(itemId, out item))
								{
									item = new CachedItem(this)
										{
											Name = reader.GetString(0),
											DatabaseName = _databaseName,
											Id = new ID(reader.GetGuid(1)),
											ParentId = new ID(reader.GetGuid(2)),
											TemplateId = new ID(reader.GetGuid(3)),
											TemplateName = reader.GetString(4),
										};

									_itemsById.Add(itemId, item);

									if (!_childIndex.ContainsKey(item.ParentId)) _childIndex[item.ParentId] = new List<ID>();

									_childIndex[item.ParentId].Add(item.Id);
								}
							}
						}
					}

					using (var fieldsCommand = new SqlCommand(fieldsSql, connection))
					{
						using (var reader = fieldsCommand.ExecuteReader())
						{
							while (reader.Read())
							{
								var itemId = new ID(reader.GetGuid(0));
								var language = reader.GetString(1);
								var versionNumber = reader.GetInt32(2);
								var fieldId = new ID(reader.GetGuid(3));
								var value = reader.GetString(4);

								var existingItem = _itemsById[itemId];
								var version = existingItem.Versions.FirstOrDefault(x => x.Language.Equals(language) && x.Version == versionNumber);

								if (version == null)
								{
									version = new CachedVersion { Language = language, Version = versionNumber };
									existingItem.Versions.Add(version);
								}

								if (fieldId == FieldIDs.Updated)
									version.Modified = DateUtil.IsoDateToDateTime(value);

								if (fieldId == FieldIDs.Revision)
									version.Revision = value;
							}
						}
					}
				}
			}

			public ISourceItem[] GetChildren(CachedItem parent)
			{
				IList<ID> children;
				if (!_childIndex.TryGetValue(parent.Id, out children)) return new ISourceItem[0];

				return children.Select(x => (ISourceItem)_itemsById[x]).ToArray();
			}

			public ISourceItem GetItemById(ID id)
			{
				CachedItem item;
				if (_itemsById.TryGetValue(id, out item)) return item;

				return null;
			}

			public ISourceItem GetItemByPath(string path)
			{
				return _itemsById.Values.FirstOrDefault(x => x.ItemPath.Equals(path));
			}

			public string GetPath(CachedItem item)
			{
				string path;
				if (_paths.TryGetValue(item.Id, out path)) return path;

				var parentIds = new HashSet<ID>();
				var pathElements = new List<string>();
				var parent = item;

				while (!parentIds.Contains(parent.Id))
				{
					pathElements.Add(parent.Name);
					parentIds.Add(parent.Id);

					if (!_itemsById.TryGetValue(parent.ParentId, out parent)) break;
				}

				pathElements.Reverse();

				var result = "/" + string.Join("/", pathElements);

				_paths.Add(item.Id, result);

				return result;
			}
		}

		[DebuggerDisplay("{ItemPath}")]
		private class CachedItem : ISourceItem
		{
			private readonly CachedDatabase _database;

			public CachedItem(CachedDatabase database)
			{
				_database = database;
				Versions = new List<CachedVersion>();
			}

			public string Name { get; set; }

			public string ItemPath
			{
				get { return _database.GetPath(this); }
			}

			public string DatabaseName { get; set; }

			public ID Id { get; set; }

			public ID ParentId { get; set; }

			public string TemplateName { get; set; }

			public ID TemplateId { get; set; }

			public string DisplayIdentifier
			{
				get { return string.Format("{0}:{1} ({2})", DatabaseName, ItemPath, Id); }
			}

			public void Recycle()
			{
				var sitecoreItem = new SitecoreSourceItem(Factory.GetDatabase(DatabaseName).GetItem(Id));

				sitecoreItem.Recycle();
			}

			public DateTime? GetLastModifiedDate(string languageCode, int versionNumber)
			{
				var version = GetVersion(languageCode, versionNumber);

				if (version == null) return null;

				return version.Modified;
			}

			public string GetRevision(string languageCode, int versionNumber)
			{
				var version = GetVersion(languageCode, versionNumber);

				if (version == null) return null;

				return version.Revision;
			}

			public ISourceItem[] Children
			{
				get { return _database.GetChildren(this); }
			}

			private CachedVersion GetVersion(string languageCode, int versionNumber)
			{
				return Versions.FirstOrDefault(x => x.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase) && x.Version == versionNumber);
			}

			public IList<CachedVersion> Versions { get; private set; }
		}

		[DebuggerDisplay("{Language} #{Version}")]
		private class CachedVersion
		{
			public string Revision { get; set; }
			public int Version { get; set; }
			public string Language { get; set; }
			public DateTime? Modified { get; set; }
		}

	}
}
