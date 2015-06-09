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
		private readonly IDataStore _serializationStore;
		private readonly ISourceDataStore _sourceDataProvider;
		private readonly ILogger _logger;

		public PredicateRootPathResolver(IPredicate predicate, IDataStore serializationStore, ISourceDataStore sourceDataProvider, ILogger logger)
		{
			_predicate = predicate;
			_serializationStore = serializationStore;
			_sourceDataProvider = sourceDataProvider;
			_logger = logger;
		}

		public ISerializableItem[] GetRootSourceItems()
		{
			var items = new List<ISerializableItem>();

			foreach (var include in _predicate.GetRootPaths())
			{
				var item = _sourceDataProvider.GetByPath(include.Database, include.Path);

				if (item != null) items.Add(item);
				else _logger.Error("Unable to resolve root source item for predicate root path {0}:{1}. It has been skipped.".FormatWith(include.Database, include.Path));
			}

			return items.ToArray();
		}

		public ISerializableItem[] GetRootSerializedItems()
		{
			var items = new List<ISerializableItem>();

			foreach (var include in _predicate.GetRootPaths())
			{
				var item = _serializationStore.GetByPath(include.Path, include.Database).ToArray();

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
