using System.Collections.Generic;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Predicates
{
	public class PredicateRootPathResolver
	{
		private readonly IPredicate _predicate;
		private readonly ISerializationProvider _serializationProvider;
		private readonly ISourceDataProvider _sourceDataProvider;

		public PredicateRootPathResolver(IPredicate predicate, ISerializationProvider serializationProvider, ISourceDataProvider sourceDataProvider)
		{
			_predicate = predicate;
			_serializationProvider = serializationProvider;
			_sourceDataProvider = sourceDataProvider;
		}

		public ISourceItem[] GetRootSourceItems()
		{
			var items = new List<ISourceItem>();

			foreach (var include in _predicate.GetRootPaths())
			{
				var item = _sourceDataProvider.GetItemByPath(include.Database, include.Path);

				if (item != null) items.Add(item);
				else Log.Warn("Unable to resolve root item for serialization preset {0}:{1}".FormatWith(include.Database, include.Path), this);
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
				else Log.Warn("Unable to resolve root item for serialization preset {0}:{1}".FormatWith(include.Database, include.Path), this);
			}

			return items.ToArray();
		}
	}
}
