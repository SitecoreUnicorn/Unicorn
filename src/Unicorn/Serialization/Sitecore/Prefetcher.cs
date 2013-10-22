using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Data;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;

namespace Unicorn.Serialization.Sitecore.Prefetcher
{
	internal class Prefetcher
	{
		private readonly Func<string, ISerializedItem> _loadMethod;
		private volatile Dictionary<ID, ISerializedItem> _idLookup = new Dictionary<ID, ISerializedItem>();
		private volatile Dictionary<ID, IList<ISerializedItem>> _childrenLookup = new Dictionary<ID, IList<ISerializedItem>>();

		public Prefetcher(Func<string, ISerializedItem> loadMethod)
		{
			_loadMethod = loadMethod;
		}

		public ISerializedItem GetItem(ID id)
		{
			Assert.ArgumentNotNull(id, "id");

			ISerializedItem resultItem;
			if (!_idLookup.TryGetValue(id, out resultItem)) return null;

			return resultItem;
		}

		public ISerializedItem[] GetChildren(ID id)
		{
			Assert.ArgumentNotNull(id, "id");

			IList<ISerializedItem> resultItems;
			if (!_childrenLookup.TryGetValue(id, out resultItems)) return null;

			return resultItems.ToArray();
		}

		public void UpdateIndex(ISerializedItem item)
		{
			_idLookup[item.Id] = item;

			if (item.ParentId != (ID)null)
			{
				if (!_childrenLookup.ContainsKey(item.Id))
					_childrenLookup[item.Id] = new List<ISerializedItem>();

				_childrenLookup[item.Id].Add(item);
			}
		}

		public void ResetPrefetch()
		{
			_childrenLookup.Clear();
			_idLookup.Clear();
		}

		public void PrefetchTree(ISerializedReference root)
		{
			Assert.ArgumentNotNull(root, "root");

			var directoryPath = SerializationPathUtility.GetReferenceDirectoryPath(root);
			var filePath = SerializationPathUtility.GetReferenceItemPath(root);
			var directoryExists = Directory.Exists(directoryPath);
			var fileExists = File.Exists(filePath);

			if (!directoryExists && !fileExists)
			{
				throw new InvalidOperationException("Root path did not exist as a file or directory!");
			}

			var files = Directory.GetFiles(directoryPath, string.Format("*{0}", PathUtils.Extension), SearchOption.AllDirectories);

			// add root item if it exists
			if (fileExists) files = files.Concat(new[] { filePath }).ToArray();

			var writeLock = new object();
			Parallel.ForEach(files, subPath =>
			{
				var item = _loadMethod(subPath);
				if (item != null)
				{
					lock (writeLock)
					{
						UpdateIndex(item);
					}
				}
			});
		}
	}
}
