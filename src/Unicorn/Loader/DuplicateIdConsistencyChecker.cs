using System.Collections.Generic;
using Rainbow.Model;
using Unicorn.Data;

namespace Unicorn.Loader
{
	/// <summary>
	/// Watches for a loader operation to attempt to load the same ID more than once
	/// </summary>
	public class DuplicateIdConsistencyChecker : IConsistencyChecker
	{
		private readonly IDuplicateIdConsistencyCheckerLogger _logger;
		private readonly Dictionary<string, DuplicateIdEntry> _duplicateChecks = new Dictionary<string, DuplicateIdEntry>();
		private readonly object _syncRoot = new object();

		public DuplicateIdConsistencyChecker(IDuplicateIdConsistencyCheckerLogger logger)
		{
			_logger = logger;
		}

		public bool IsConsistent(IItemData itemData)
		{
			DuplicateIdEntry duplicateItemData;
			lock (_syncRoot)
			{
				if (!_duplicateChecks.TryGetValue(CreateKey(itemData), out duplicateItemData)) return true;
			}

			_logger.DuplicateFound(duplicateItemData, itemData);

			return false;
		}

		public void AddProcessedItem(IItemData itemData)
		{
			lock (_syncRoot)
			{
				_duplicateChecks.Add(CreateKey(itemData), new DuplicateIdEntry(itemData));
			}
		}

		protected virtual string CreateKey(IItemData itemData)
		{
			return itemData.Id + itemData.DatabaseName;
		}

		public class DuplicateIdEntry
		{
			public DuplicateIdEntry(IItemData item)
			{
				DisplayName = item.GetDisplayIdentifier();
				SerializedItemId = item.SerializedItemId;
			}

			public string DisplayName { get; private set; }
			public string SerializedItemId { get; private set; }
		}
	}
}
