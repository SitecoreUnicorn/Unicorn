using System.Collections.Generic;
using Rainbow.Model;

namespace Unicorn.Loader
{
	public class DuplicateIdConsistencyChecker : IConsistencyChecker
	{
		private readonly IDuplicateIdConsistencyCheckerLogger _logger;
		private readonly Dictionary<string, ISerializableItem> _duplicateChecks = new Dictionary<string, ISerializableItem>();

		public DuplicateIdConsistencyChecker(IDuplicateIdConsistencyCheckerLogger logger)
		{
			_logger = logger;
		}

		public bool IsConsistent(ISerializableItem item)
		{
			ISerializableItem duplicateItem;
			if(!_duplicateChecks.TryGetValue(CreateKey(item), out duplicateItem)) return true;

			_logger.DuplicateFound(duplicateItem, item);

			return false;
		}

		public void AddProcessedItem(ISerializableItem item)
		{
			_duplicateChecks.Add(CreateKey(item), item);
		}

		protected virtual string CreateKey(ISerializableItem item)
		{
			return item.Id + item.DatabaseName;
		}
	}
}
