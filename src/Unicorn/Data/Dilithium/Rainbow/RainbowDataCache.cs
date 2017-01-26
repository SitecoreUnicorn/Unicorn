using System;
using System.Collections.Generic;
using Sitecore.Data;
using Sitecore.Diagnostics;

// ReSharper disable TooWideLocalVariableScope

namespace Unicorn.Data.Dilithium.Rainbow
{
	public class RainbowDataCache
	{
		private readonly Dictionary<Guid, RainbowItemData> _itemsById = new Dictionary<Guid, RainbowItemData>(8087); // start with large capacity. Dictionary likes primes for its size.
		private Dictionary<string, IList<RainbowItemData>> _itemsByPath;

		public RainbowDataCache(string databaseName)
		{
			Database = Database.GetDatabase(databaseName);
			Assert.ArgumentNotNull(Database, nameof(databaseName));
		}

		public Database Database { get; }

		public int Count => _itemsById.Count;

		public IEnumerable<RainbowItemData> GetChildren(RainbowItemData item)
		{
			return GuidsToItems(item.Children);
		}

		public IEnumerable<RainbowItemData> GetByPath(string path)
		{
			IList<RainbowItemData> itemsAtPath;

			if(!_itemsByPath.TryGetValue(path, out itemsAtPath)) return new RainbowItemData[0];

			return itemsAtPath;
		}

		public RainbowItemData GetById(Guid id)
		{
			RainbowItemData item;
			if (_itemsById.TryGetValue(id, out item)) return item;

			return null;
		}

		public void AddItem(RainbowItemData item)
		{
			_itemsById.Add(item.Id, item);
		}

		public void Ingest()
		{
			IndexChildren();
			IndexPaths();
		}

		private void IndexPaths()
		{
			var pathIndex = new Dictionary<string, IList<RainbowItemData>>(StringComparer.Ordinal);
			var itemsById = _itemsById;
			RainbowItemData currentItem;
			IList<RainbowItemData> pathItemList;

			foreach(var itemKey in itemsById)
			{
				currentItem = itemKey.Value;

				if (!pathIndex.TryGetValue(currentItem.Path, out pathItemList))
				{
					pathItemList = pathIndex[currentItem.Path] = new List<RainbowItemData>();
				}

				pathItemList.Add(currentItem);
			}

			_itemsByPath = pathIndex;
		}

		private void IndexChildren()
		{
			RainbowItemData currentItem;
			RainbowItemData parentItem;
			var itemsById = _itemsById;

			// puts the IDs of all children into each item's Children list
			foreach (var itemById in itemsById)
			{
				currentItem = itemById.Value;

				if (itemsById.TryGetValue(currentItem.ParentId, out parentItem))
				{
					parentItem.Children.Add(currentItem.Id);
				}
			}
		}

		private IList<RainbowItemData> GuidsToItems(IList<Guid> guids)
		{
			var items = new List<RainbowItemData>(guids.Count);

			foreach (var childId in guids)
			{
				items.Add(_itemsById[childId]);
			}

			return items;
		}
	}
}
