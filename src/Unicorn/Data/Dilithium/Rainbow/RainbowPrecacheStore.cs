using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage;
using Unicorn.Configuration;

// ReSharper disable TooWideLocalVariableScope

namespace Unicorn.Data.Dilithium.Rainbow
{
	public class RainbowPrecacheStore
	{
		protected object SyncLock = new object();
		protected bool Initialized = false;

		private readonly IConfiguration[] _configurations;
		private Dictionary<string, RainbowDataCache> _itemCores; 

		public RainbowPrecacheStore(IConfiguration[] configurations)
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
			var dilithiumItem = item as RainbowItemData;

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

		/// <summary>
		/// Sets up Dilithium's cache for all configurations passed in, if they use the DilithiumDataStore.
		/// </summary>
		/// <param name="force">Force reinitialization (reread from data store)</param>
		/// <returns>True if initialized successfully (or if already inited), false if no configurations were using Dilithium</returns>
		public InitResult Initialize(bool force)
		{
			if (Initialized && !force) return new InitResult(false);

			lock (SyncLock)
			{
				if (Initialized && !force) return new InitResult(false);

				var timer = new Stopwatch();
				timer.Start();

				int itemsLoaded = 0;
				var caches = new Dictionary<string, RainbowDataCache>(StringComparer.Ordinal);
				RainbowDataCache currentCache;

				ConfigurationDataStore targetDataStore;
				ISnapshotCapableDataStore snapshotDataStore;

				foreach (var configuration in _configurations)
				{
					// check that config is using Dilithium (if not we don't need to load it)
					targetDataStore = configuration.Resolve<ITargetDataStore>() as ConfigurationDataStore;
					snapshotDataStore = targetDataStore?.InnerDataStore as DilithiumSerializationFileSystemDataStore;

					if (snapshotDataStore == null) continue; // can't snapshot this data store so do nothing

					var snapshotItems = snapshotDataStore.GetSnapshot();

					foreach (var item in snapshotItems)
					{
						if (!caches.TryGetValue(item.DatabaseName, out currentCache))
						{
							caches.Add(item.DatabaseName, currentCache = new RainbowDataCache(item.DatabaseName));
						}

						if (currentCache.AddItem(new RainbowItemData(item, currentCache)))
						{
							// If different configs have the same item, the item will be 'added' twice in different
							// snapshots if those configs sync together.
							// If we did not only count 'new' items the counts shown for items loaded might not match
							// between DiSql and DiSfs, which might cause undue panic from a user even though it's totally fine internally
							itemsLoaded++;
						}
					}
				}

				// index items that have been added
				foreach (var cache in caches)
				{
					cache.Value.Ingest();
				}

				Initialized = true;
				_itemCores = caches;

				if (caches.Count == 0)
				{
					return new InitResult(false);
				}

				timer.Stop();

				return new InitResult(true, false, itemsLoaded, (int)timer.ElapsedMilliseconds);
			}
		}

		private RainbowDataCache GetCore(string database)
		{
			RainbowDataCache cache;

			if (_itemCores.TryGetValue(database, out cache)) return cache;

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
