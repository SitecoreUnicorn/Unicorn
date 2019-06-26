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
using Unicorn.Predicates;

// ReSharper disable TooWideLocalVariableScope

namespace Unicorn.Data.Dilithium.Sql
{
	public class SqlPrecacheStore
	{
		protected object SyncLock = new object();
		protected bool Initialized = false;

		private readonly IConfiguration[] _configurations;
		private Dictionary<string, SqlDataCache> _databaseCores; 

		public SqlPrecacheStore(IConfiguration[] configurations)
		{
			_configurations = configurations;
		}

		public IEnumerable<IItemData> GetByPath(string path, string database)
		{
			var core = GetCore(database);

			if (core == null) return Enumerable.Empty<IItemData>();

			return core.GetByPath(path);
		}

		public IEnumerable<IItemData> GetChildren(IItemData item)
		{
			var dilithiumItem = item as SqlItemData;

			// if the item is not from Dilithium it will have to use its original data store to get children
			if (dilithiumItem == null) return item.GetChildren();

			var core = GetCore(item.DatabaseName);

			if (core == null) return Enumerable.Empty<IItemData>();

			return core.GetChildren(dilithiumItem);
		}

		public IItemData GetById(Guid id, string database)
		{
			var core = GetCore(database);

			return core?.GetById(id);
		}

		public void Update(IItemData item)
		{
			var core = GetCore(item.DatabaseName);

			core?.Update(item);
		}

		public void Remove(IItemData item)
		{
			var core = GetCore(item.DatabaseName);

			core?.Remove(item);
		}

		/// <summary>
		/// Sets up Dilithium's cache for all configurations passed in, if they use the DilithiumDataStore.
		/// </summary>
		/// <param name="force">Force reinitialization (reread from SQL)</param>
		/// <param name="specificRoots">If passed, overrides the predicate roots in the configurations registered. Used for partial sync/partial reserialize.</param>
		/// <returns>True if initialized successfully (or if already inited), false if no configurations were using Dilithium</returns>
		public InitResult Initialize(bool force, params IItemData[] specificRoots)
		{
			if (Initialized && !force) return new InitResult(false);

			lock (SyncLock)
			{
				if (Initialized && !force) return new InitResult(false);

				var timer = new Stopwatch();
				timer.Start();

				// if specific roots are passed we init the collection with them
				var allPredicateRoots = new List<TreeRoot>(specificRoots.Select(root => new TreeRoot(string.Empty, root.Path, root.DatabaseName)));

				HashSet<Guid> intersectedIgnoredFields = null;

				IPredicate predicate;
				ConfigurationDataStore sourceDataStore;
				IEnumerableFieldFilter fieldFilter;

				foreach (var configuration in _configurations)
				{
					// check that config is using Dilithium (if not we don't need to load it)
					sourceDataStore = configuration.Resolve<ISourceDataStore>() as ConfigurationDataStore;
					if (!(sourceDataStore?.InnerDataStore is DilithiumSitecoreDataStore)) continue;

					// unless specific roots are passed we add all predicate roots
					if (specificRoots.Length == 0)
					{
						// add configuration's predicate roots to the pile of dilithium store roots
						predicate = configuration.Resolve<IPredicate>();
						allPredicateRoots.AddRange(predicate.GetRootPaths());
					}

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

					_databaseCores = new Dictionary<string, SqlDataCache>();

					return new InitResult(false);
				}

				// calculate root path uniqueness (e.g. if /sitecore/templates and /sitecore/templates/foo are both here
				// we must remove /sitecore/templates/foo because Dilithium is strictly descendants and follows no exclusions)
				for (var index = allPredicateRoots.Count - 1; index >= 0; index--)
				{
					var compareAgainstItem = allPredicateRoots[index];
					var compareAgainstPath = compareAgainstItem.Path + "/";
					for (var longerIndex = allPredicateRoots.Count - 1; longerIndex >= 0; longerIndex--)
					{
						var longerIndexItem = allPredicateRoots[longerIndex];

						if (longerIndexItem.DatabaseName.Equals(compareAgainstItem.DatabaseName) &&
							longerIndexItem.Path.StartsWith(compareAgainstPath, StringComparison.OrdinalIgnoreCase))
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

					// if the root item is null, its path does not exist in the DB.
					// so we'll just not add it, and items will return null
					// which is correct, since we want those items to get created from serialized
					if (rootItem != null)
					{
						databases[root.DatabaseName].Add(new RootData(root.Path, rootItem.ID.Guid));
					}
				}

				// generate a data core for each database, which contains all the predicated items' item data
				var dataCores = new Dictionary<string, SqlDataCache>(databases.Count, StringComparer.Ordinal);
				bool coreLoadError = false;
				foreach (var database in databases)
				{
					var dataCore = new SqlDataCache(database.Key);

					var rootIds = database.Value.Select(v => v.Id).ToArray();

					if (rootIds.Length == 0) continue;

					using (var sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings[database.Key].ConnectionString))
					{
						sqlConnection.Open();
						using (var sqlCommand = ConstructSqlBatch(rootIds, intersectedIgnoredFields?.ToArray()))
						{
							sqlCommand.Connection = sqlConnection;

							using (var reader = sqlCommand.ExecuteReader())
							{
								if (!dataCore.Ingest(reader, database.Value)) coreLoadError = true;
							}
						}
					}

					dataCores.Add(database.Key, dataCore);
				}

				timer.Stop();

				Initialized = true;
				_databaseCores = dataCores;

				return new InitResult(true, coreLoadError, dataCores.Values.Select(core => core.Count).Sum(), (int)timer.ElapsedMilliseconds);
			}
		}

		private SqlCommand ConstructSqlBatch(Guid[] rootItemIds, Guid[] ignoredFields)
		{
			Assert.ArgumentNotNull(rootItemIds, nameof(rootItemIds));
			if (rootItemIds.Length == 0) throw new InvalidOperationException("Cannot make a query for empty root set. This likely means a predicate did not have any roots.");
			if (ignoredFields == null) ignoredFields = new Guid[0];

			var command = new SqlCommand();
			var debugCommand = new StringBuilder();

			// add parameters for ignored fields
			var ignoredFieldsInStatement = BuildSqlInStatement(ignoredFields, command, "i", debugCommand);

			var ignoredFieldsValueSkipStatement = $@"CASE WHEN FieldID {ignoredFieldsInStatement} THEN '' ELSE Value END AS Value";

			// add parameters for root item IDs
			var rootItemIdsInStatement = BuildSqlInStatement(rootItemIds, command, "r", debugCommand);

			var sql = new StringBuilder(8000);

			// ITEM DATA QUERY - gets top level metadata about included items (no fields)
			sql.Append($@"
				IF OBJECT_ID('tempdb..#TempItemData') IS NOT NULL DROP Table #TempItemData

				CREATE TABLE #TempItemData(
					 ID uniqueidentifier,
					 Name nvarchar(256),
					 TemplateID uniqueidentifier,
					 MasterID uniqueidentifier,
					 ParentID uniqueidentifier
				 );

				WITH Roots AS (
					SELECT Id
					FROM Items
					WHERE ID {rootItemIdsInStatement}
				), tree AS (
					SELECT x.ID, x.Name, x.TemplateID, x.MasterID, x.ParentID
					FROM Items x
					INNER JOIN Roots ON x.ID = Roots.ID
					UNION ALL
					SELECT y.ID, y.Name, y.TemplateID, y.MasterID, y.ParentID
					FROM Items y
					INNER JOIN tree t ON y.ParentID = t.ID
				)
				INSERT INTO #TempItemData
				SELECT *
				FROM tree

				SELECT ID, Name, TemplateID, MasterID, ParentID
				FROM #TempItemData
");

			// FIELDS DATA QUERY - gets all fields for all languages and versions of the root items and all descendants
			sql.Append($@"
				SELECT ItemId, '' AS Language, FieldId, {ignoredFieldsValueSkipStatement}, -1 as Version
				FROM SharedFields s
				INNER JOIN #TempItemData t ON s.ItemId = t.ID
				UNION ALL
				SELECT ItemId, Language, FieldId, {ignoredFieldsValueSkipStatement}, -1 as Version
				FROM UnversionedFields u
				INNER JOIN #TempItemData t ON u.ItemId = t.ID
				UNION ALL
				SELECT ItemId, Language, FieldId, {ignoredFieldsValueSkipStatement}, Version
				FROM VersionedFields v
				INNER JOIN #TempItemData t ON v.ItemId = t.ID
");

			command.CommandText = sql.ToString();

			debugCommand.Append(sql);

			// drop a debugger on this to see a runnable SQL statement for SSMS
			var debugSqlStatement = debugCommand.ToString();

			return command;
		}

		private StringBuilder BuildSqlInStatement(Guid[] parameters, SqlCommand command, string parameterPrefix, StringBuilder debugStatementBuilder)
		{
			object currentParameter;
			string parameterName;

			var parameterNames = new List<string>(parameters.Length);
			
			for (int index = 0; index < parameters.Length; index++)
			{
				currentParameter = parameters[index];
				parameterName = parameterPrefix + index;

				command.Parameters.AddWithValue(parameterName, currentParameter);
				debugStatementBuilder.AppendLine($"DECLARE @{parameterName} UNIQUEIDENTIFIER = '{currentParameter}'");
				parameterNames.Add(parameterName);
			}

			var inStatement = new StringBuilder(((parameterPrefix.Length + 4) * parameters.Length) + 5); // ((prefixLength + '@, ') * paramCount) + 'IN ()'
			inStatement.Append("IN (");
			inStatement.Append("@"); // first element param @, subsequent get from join below
			inStatement.Append(string.Join(", @", parameterNames));
			inStatement.Append(")");

			return inStatement;
		}

		private SqlDataCache GetCore(string database)
		{
			SqlDataCache cache;

			if (_databaseCores.TryGetValue(database, out cache)) return cache;

			return null;
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
