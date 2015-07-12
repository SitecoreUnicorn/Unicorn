using System.Collections.Generic;
using Rainbow.Model;

namespace Unicorn.Loader
{
	public class DuplicateIdConsistencyChecker : IConsistencyChecker
	{
		private readonly IDuplicateIdConsistencyCheckerLogger _logger;
		private readonly Dictionary<string, IItemData> _duplicateChecks = new Dictionary<string, IItemData>();

		public DuplicateIdConsistencyChecker(IDuplicateIdConsistencyCheckerLogger logger)
		{
			_logger = logger;
		}

		public bool IsConsistent(IItemData itemData)
		{
			IItemData duplicateItemData;
			if(!_duplicateChecks.TryGetValue(CreateKey(itemData), out duplicateItemData)) return true;

			_logger.DuplicateFound(duplicateItemData, itemData);

			return false;
		}

		public void AddProcessedItem(IItemData itemData)
		{
			_duplicateChecks.Add(CreateKey(itemData), itemData);
		}

		protected virtual string CreateKey(IItemData itemData)
		{
			return itemData.Id + itemData.DatabaseName;
		}
	}
}
