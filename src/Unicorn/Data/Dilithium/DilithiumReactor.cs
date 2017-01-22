using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rainbow.Filtering;
using Rainbow.Model;
using Rainbow.Storage;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Unicorn.Configuration;
using Unicorn.Data.Dilithium.Data;
using Unicorn.Predicates;
// ReSharper disable TooWideLocalVariableScope

namespace Unicorn.Data.Dilithium
{
	public class DilithiumReactor
	{
		protected object SyncLock = new object();
		protected bool Initialized = false;

		private readonly IConfiguration[] _configurations;
		private List<DataCore> _dataCores; 

		public DilithiumReactor(IConfiguration[] configurations)
		{
			_configurations = configurations;
		}

		public IEnumerable<IItemData> GetByPath(string path, string database)
		{
			var cores = GetCores(database);

			var results = new List<IItemData>();
			foreach (var core in cores)
			{
				results.AddRange(core.GetByPath(path));
			}

			return results;
		}

		public IEnumerable<IItemData> GetChildren(IItemData item)
		{
			var dilithiumItem = item as DilithiumItemData;

			if(dilithiumItem == null) throw new InvalidOperationException($"Dilithium only knows how to get children of items returned from Dilithium. {item.GetType().FullName} is incompatible.");

			var cores = GetCores(item.DatabaseName);

			var results = new List<IItemData>();
			foreach (var core in cores)
			{
				results.AddRange(core.GetChildren(dilithiumItem));
			}

			return results;
		}

		public IItemData GetById(Guid id, string database)
		{
			var cores = GetCores(database);
			DilithiumItemData result;

			foreach (var core in cores)
			{
				result = core.GetById(id);

				if (result != null) return result;
			}

			return null;
		}

		/// <summary>
		/// Sets up Dilithium's cache for all configurations passed in, if they use the DilithiumDataStore.
		/// </summary>
		/// <param name="force">Force reinitialization (reread from SQL)</param>
		/// <returns>True if initialized successfully (or if already inited), false if no configurations were using Dilithium</returns>
		public bool Initialize(bool force)
		{
			if (Initialized && !force) return true;

			lock (SyncLock)
			{
				if (Initialized && !force) return true;

				var timer = new Stopwatch();
				timer.Start();

				var allPredicateRoots = new List<TreeRoot>();
				HashSet<Guid> intersectedIgnoredFields = null;

				IPredicate predicate;
				ConfigurationDataStore sourceDataStore;
				IEnumerableFieldFilter fieldFilter;

				foreach (var configuration in _configurations)
				{
					// check that config is using Dilithium (if not we don't need to load it)
					sourceDataStore = configuration.Resolve<ISourceDataStore>() as ConfigurationDataStore;
					if (!(sourceDataStore?.InnerDataStore is DilithiumSitecoreDataStore)) continue;

					// add configuration's predicate roots to the pile of dilithium store roots
					predicate = configuration.Resolve<IPredicate>();
					allPredicateRoots.AddRange(predicate.GetRootPaths());

					// acquire list of ignored fields for configuration
					fieldFilter = configuration.Resolve<IEnumerableFieldFilter>();
					
					// if there is no field filter we can enumerate, then we must assume all fields are included.
					// defining the hashset here will prevent anything from intersecting with it later.
					if(fieldFilter == null) intersectedIgnoredFields = new HashSet<Guid>();

					if (intersectedIgnoredFields == null)
					{
						// if set not started, all initial values go into it for intersecting with later configsets
						// we want to end up with fields that _all_ configs ignore that we care about
						intersectedIgnoredFields = new HashSet<Guid>(fieldFilter.Excludes);
					}
					// already have some fields, so we intersect with the current field filter to produce only those in both sets
					else if (fieldFilter != null)
					{
						intersectedIgnoredFields.IntersectWith(fieldFilter.Excludes);
					}
				}

				// no configurations were initialized that included Dilithium
				if (allPredicateRoots.Count == 0)
				{
					Initialized = true;
					_dataCores = new List<DataCore>();
					return false;
				}

				// calculate root path uniqueness (e.g. if /sitecore/templates and /sitecore/templates/foo are both here
				// we must remove /sitecore/templates/foo because Dilithium is strictly descendants and follows no exclusions)
				for (var index = allPredicateRoots.Count - 1; index >= 0; index--)
				{
					var compareAgainst = allPredicateRoots[index].Path + "/";
					for (var longerIndex = allPredicateRoots.Count - 1; longerIndex >= 0; longerIndex--)
					{
						if (allPredicateRoots[longerIndex].Path.StartsWith(compareAgainst, StringComparison.OrdinalIgnoreCase))
						{
							allPredicateRoots.RemoveAt(longerIndex);

							index++;
							if (index >= allPredicateRoots.Count) index = allPredicateRoots.Count - 1;

							break;
						}
					}
				}

				// generate unique database names involved and roots within
				var databases = new Dictionary<string, List<RootData>>();
				foreach (var root in allPredicateRoots)
				{
					if (!databases.ContainsKey(root.DatabaseName)) databases.Add(root.DatabaseName, new List<RootData>());

					var rootItem = Factory.GetDatabase(root.DatabaseName).GetItem(root.Path);

					// TODO ??? should this be an error or just don't add the root thus causing it to not exist in the store (e.g. serialized needs to write to it on sync)
					if (rootItem == null) throw new InvalidOperationException($"Cannot resolve root path {root.Path} in {root.DatabaseName} Sitecore database. Check your predicates.");

					databases[root.DatabaseName].Add(new RootData(root.Path, rootItem.ID.Guid));
				}

				// generate a data core for each database, which contains all the predicated items' item data
				var dataCores = new List<DataCore>(databases.Count);
				foreach (var database in databases)
				{
					var dataCore = new DataCore(database.Key);
					using (var sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings[database.Key].ConnectionString))
					{
						sqlConnection.Open();
						using (var sqlCommand = ConstructSqlBatch(database.Value.Select(v => v.Id).ToArray(), intersectedIgnoredFields?.ToArray()))
						{
							sqlCommand.Connection = sqlConnection;

							using (var reader = sqlCommand.ExecuteReader())
							{
								dataCore.Ingest(reader, database.Value);
							}
						}
					}

					dataCores.Add(dataCore);
				}

				timer.Stop();
				Log.Info($"[Unicorn] Initialized Dilithium reactor with {dataCores.Count} cores. {dataCores.Select(core => core.Count).Sum()} total items in reactor cores. Initialized in {timer.ElapsedMilliseconds} ms", this);

				Initialized = true;
				_dataCores = dataCores;
				return true;
			}
		}

		private SqlCommand ConstructSqlBatch(Guid[] rootItemIds, Guid[] ignoredFields)
		{
			Assert.ArgumentNotNull(rootItemIds, nameof(rootItemIds));
			if (rootItemIds.Length == 0) throw new InvalidOperationException("Cannot make a query for empty root set.");
			if (ignoredFields == null) ignoredFields = new Guid[0];

			var command = new SqlCommand();

			// add parameters for ignored fields
			var ignoredFieldsInStatement = BuildSqlInStatement(ignoredFields, command, "i");

			// add parameters for root item IDs
			var rootItemIdsInStatement = BuildSqlInStatement(rootItemIds, command, "r");

			var sql = new StringBuilder(8000);

			// ITEM DATA QUERY - gets top level metadata about included items (no fields)
			sql.Append($@"
				SELECT i.ID, i.Name, i.TemplateID, i.MasterID, i.ParentID
				FROM Items i
				WHERE i.ID {rootItemIdsInStatement}
				UNION ALL
				SELECT i.ID, i.Name, i.TemplateID, i.MasterID, i.ParentID
				FROM Items i
				INNER JOIN Descendants d on i.ID = d.Descendant
				WHERE d.Ancestor {rootItemIdsInStatement}
");

			// FIELDS DATA QUERY - DESCENDANTS - gets all fields for all languages and versions of *descendants*
			// of the root items. Does not get the root items' fields data (it seemed faster to do that in a separate
			// query based on query analysis as opposed to an IN...OR...IN in this one)
			sql.Append($@"
				SELECT ItemId, '' AS Language, FieldId, Value, -1 as Version
				FROM SharedFields s
				INNER JOIN Descendants d on s.ItemId = d.Descendant
				WHERE d.Ancestor {rootItemIdsInStatement}
				AND s.FieldId NOT {ignoredFieldsInStatement}
				UNION ALL
				SELECT ItemId, Language, FieldId, Value, -1 as Version
				FROM UnversionedFields u
				INNER JOIN Descendants d on u.ItemId = d.Descendant
				WHERE d.Ancestor {rootItemIdsInStatement}
				AND u.FieldId NOT {ignoredFieldsInStatement}
				UNION ALL
				SELECT v.ItemId, v.Language, v.FieldId, v.Value, v.Version
				FROM VersionedFields v
				INNER JOIN Descendants d on v.ItemId = d.Descendant
				WHERE d.Ancestor {rootItemIdsInStatement}
				AND v.FieldId NOT {ignoredFieldsInStatement}
");

			// FIELDS DATA QUERY - ROOTS - gets all fields for all languages and versions of the root items
			sql.Append($@"
				SELECT ItemId, '' AS Language, FieldId, Value, -1 as Version
				FROM SharedFields s
				WHERE s.ItemId {rootItemIdsInStatement}
				AND s.FieldId NOT {ignoredFieldsInStatement}
				UNION ALL
				SELECT ItemId, Language, FieldId, Value, -1 as Version
				FROM UnversionedFields u
				WHERE u.ItemId {rootItemIdsInStatement}
				AND u.FieldId NOT {ignoredFieldsInStatement}
				UNION ALL
				SELECT v.ItemId, v.Language, v.FieldId, v.Value, v.Version
				FROM VersionedFields v
				WHERE v.ItemId {rootItemIdsInStatement}
				AND v.FieldId NOT {ignoredFieldsInStatement}
");

			// EMPTY VERSIONS QUERY - Finds all versions of an item who have ONLY ignored fields in them
			// this is MUCH faster than including them in the fields data query (can save 200k rows for revision and modified on stock core for example, and 500ms)
			sql.Append($@"
				SELECT DISTINCT v.ItemID, v.Version, v.Language
				FROM VersionedFields v 
				INNER JOIN Descendants d on v.ItemId = d.Descendant
				WHERE d.Ancestor {rootItemIdsInStatement}
				AND FieldID {ignoredFieldsInStatement}
				AND ItemId NOT IN (SELECT v2.ItemID FROM VersionedFields v2 WHERE FieldID NOT {ignoredFieldsInStatement} AND v2.Language = v.Language AND v2.Version = v.Version)");

			command.CommandText = sql.ToString();

			return command;
		}

		private StringBuilder BuildSqlInStatement(Guid[] parameters, SqlCommand command, string parameterPrefix)
		{
			object currentParameter;
			string parameterName;

			var parameterNames = new List<string>(parameters.Length);


			for (int index = 0; index < parameters.Length; index++)
			{
				currentParameter = parameters[index];
				parameterName = parameterPrefix + index;

				command.Parameters.AddWithValue(parameterName, currentParameter);
				parameterNames.Add(parameterName);
			}

			var inStatement = new StringBuilder(((parameterPrefix.Length + 4) * parameters.Length) + 5); // ((prefixLength + '@, ') * paramCount) + 'IN ()'
			inStatement.Append("IN (");
			inStatement.Append("@"); // first element param @, subsequent get from join below
			inStatement.Append(string.Join(", @", parameterNames));
			inStatement.Append(")");

			return inStatement;
		}

		private IList<DataCore> GetCores(string database)
		{
			if (_dataCores == null) Initialize(false);
			if(_dataCores == null) return new List<DataCore>();

			var result = new List<DataCore>();
			foreach (var dataCore in _dataCores)
			{
				if (!dataCore.Database.Name.Equals(database, StringComparison.OrdinalIgnoreCase)) continue;
				
				result.Add(dataCore);
			}

			return result;
		}

		public class RootData
		{
			public string Path { get; }
			public Guid Id { get; }

			public RootData(string path, Guid id)
			{
				Path = path;
				Id = id;
			}
		}
	}
}
