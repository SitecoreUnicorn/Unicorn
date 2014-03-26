using System.Collections.Generic;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Serialization;

namespace Unicorn.Predicates
{
	public class PredicateRootPathResolver
	{
		private readonly IPredicate _predicate;
		private readonly ISerializationProvider _serializationProvider;
		private readonly ISourceDataProvider _sourceDataProvider;
		private readonly ILogger _logger;

		public PredicateRootPathResolver(IPredicate predicate, ISerializationProvider serializationProvider, ISourceDataProvider sourceDataProvider, ILogger logger)
		{
			_predicate = predicate;
			_serializationProvider = serializationProvider;
			_sourceDataProvider = sourceDataProvider;
			_logger = logger;
		}

		public ISourceItem[] GetRootSourceItems()
		{
			var items = new List<ISourceItem>();

			foreach (var include in _predicate.GetRootPaths())
			{
				var item = _sourceDataProvider.GetItemByPath(include.Database, include.Path);

				if (item != null) items.Add(item);
				else _logger.Error("Unable to resolve root source item for predicate root path {0}:{1}. It has been skipped.".FormatWith(include.Database, include.Path));
			}

			return items.ToArray();
		}

		public ISerializedItem[] GetRootSerializedItems()
		{
			var items = new List<ISerializedItem>();

			foreach (var include in _predicate.GetRootPaths())
			{
				var item = _serializationProvider.GetItemByPath(include.Database, include.Path);

				if (item != null) items.Add(item);
				else _logger.Error("Unable to resolve root serialized item for predicate root path {0}:{1}. It has been skipped.".FormatWith(include.Database, include.Path));
			}

			return items.ToArray();
		}
	}
}
