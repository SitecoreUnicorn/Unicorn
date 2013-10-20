using System.Collections.Generic;
using System.Linq;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public class DuplicateIdConsistencyChecker : IConsistencyChecker
	{
		private readonly IDuplicateIdConsistencyCheckerLogger _logger;
		private readonly Dictionary<string, ISerializedItem> _duplicateChecks = new Dictionary<string, ISerializedItem>();

		public DuplicateIdConsistencyChecker(IDuplicateIdConsistencyCheckerLogger logger)
		{
			_logger = logger;
		}

		public bool IsConsistent(ISerializedItem item)
		{
			ISerializedItem duplicateItem;
			if(!_duplicateChecks.TryGetValue(CreateKey(item), out duplicateItem)) return true;

			_logger.DuplicateFound(duplicateItem, item);

			return false;
		}

		public void AddProcessedItem(ISerializedItem item)
		{
			_duplicateChecks.Add(CreateKey(item), item);
		}

		protected virtual string CreateKey(ISerializedItem item)
		{
			return item.Id + item.DatabaseName;
		}
	}
}
