using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;

namespace Unicorn.Predicates
{
	// we'd killed IsourceDataStore. Now to fix the issues from that, and work out how we'll handle dep reg for IDataStore
	public class PredicateRootPathResolver
	{
		private readonly IPredicate _predicate;
		private readonly ITargetDataStore _targetDataStore;
		private readonly ISourceDataStore _sourceDataStore;
		private readonly ILogger _logger;

		public PredicateRootPathResolver(IPredicate predicate, ITargetDataStore targetDataStore, ISourceDataStore sourceDataStore, ILogger logger)
		{
			_predicate = predicate;
			_targetDataStore = targetDataStore;
			_sourceDataStore = sourceDataStore;
			_logger = logger;
		}

		public IItemData[] GetRootSourceItems()
		{
			var items = new List<IItemData>();

			foreach (var include in _predicate.GetRootPaths())
			{
				var item = _sourceDataStore.GetByPath(include.Path, include.Database).FirstOrDefault();

				if (item != null) items.Add(item);
				else _logger.Error("Unable to resolve root source item for predicate root path {0}:{1}. It has been skipped.".FormatWith(include.Database, include.Path));
			}

			return items.ToArray();
		}

		public IItemData[] GetRootSerializedItems()
		{
			var items = new List<IItemData>();

			foreach (var include in _predicate.GetRootPaths())
			{
				var item = _targetDataStore.GetByPath(include.Path, include.Database).ToArray();

				if (item.Length == 1)
				{
					items.Add(item[0]);
				}
				else _logger.Error("Unable to resolve root serialized item for predicate root path {0}:{1}. Either the path did not exist, or multiple items matched the path. It has been skipped.".FormatWith(include.Database, include.Path));
			}

			return items.ToArray();
		}
	}
}
