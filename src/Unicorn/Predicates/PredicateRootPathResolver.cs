using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;

namespace Unicorn.Predicates
{
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

			foreach (var include in GetRootPaths())
			{
				var item = _sourceDataStore.GetByPath(include.Path, include.DatabaseName).FirstOrDefault();

				if (item != null)
					items.Add(item);
				else
					_logger.Error("Unable to resolve root source item for predicate root path {0}:{1}. It has been skipped.".FormatWith(include.DatabaseName, include.Path));
			}

			return items.ToArray();
		}

		public TreeRoot[] GetRootPaths()
		{
			return _predicate.GetRootPaths();
		}

		public IItemData[] GetRootSerializedItems()
		{
			var items = new List<IItemData>();

			foreach (var include in _predicate.GetRootPaths())
			{
				var item = _targetDataStore.GetByPath(include.Path, include.DatabaseName).ToArray();

				if (item.Length == 1)
				{
					items.Add(item[0]);
				}
				else if (item.Length == 0)
				{
					_logger.Error("Unable to resolve serialized item for included root path {0}:{1}. The item does not exist in {2}. It has been skipped. Perhaps you need to perform an initial serialization from the control panel?".FormatWith(include.DatabaseName, include.Path, _targetDataStore.FriendlyName));
				}
				else
				{
					_logger.Error("Multiple serialized items matched included root path {0}:{1} in {2}. You cannot have a root path that is not unique. It has been skipped.".FormatWith(include.DatabaseName, include.Path, _targetDataStore.FriendlyName));
				}
			}

			return items.ToArray();
		}
	}
}
