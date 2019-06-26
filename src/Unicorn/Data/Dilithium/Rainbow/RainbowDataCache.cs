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

		public IList<RainbowItemData> GetChildren(RainbowItemData item)
		{
			return GuidsToItems(item.Children);
		}

		public IList<RainbowItemData> GetByPath(string path)
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

		public bool AddItem(RainbowItemData item)
		{
			// if multiple configurations contain the same item they may attempt to add the same item ID twice
			if (!_itemsById.ContainsKey(item.Id))
			{
				_itemsById.Add(item.Id, item);
				return true;
			}

			return false;
		}

		public void Ingest()
		{
			IndexChildren();
			IndexPaths();
		}

		private void IndexPaths()
		{
			var pathIndex = new Dictionary<string, IList<RainbowItemData>>(StringComparer.OrdinalIgnoreCase);
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
				if (!_itemsById.TryGetValue(childId, out RainbowItemData item))
				{
					Log.Debug($"[Dilithium] Child ID {childId} was not present in the items by ID index. This can occur if the item was deleted previously during a sync, in which case it's expected.");
					continue;
				}

				items.Add(item);
			}

			return items;
		}
	}
}
