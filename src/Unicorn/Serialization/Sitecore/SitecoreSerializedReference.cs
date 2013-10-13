using System.Diagnostics;
using System.Linq;
using Sitecore.Data.Serialization;

namespace Unicorn.Serialization.Sitecore
{
	[DebuggerDisplay("SerRef: {ItemPath}")]
	public class SitecoreSerializedReference : ISerializedReference
	{
		public SitecoreSerializedReference(string physicalPath)
		{
			ProviderId = physicalPath;
		}

		string _itemPath;
		public string ItemPath
		{
			get
			{
				if (_itemPath == null) LoadItemPath();
				return _itemPath;
			}
		}

		string _databaseName;
		public string DatabaseName
		{
			get
			{
				if (_databaseName == null) LoadItemPath();
				return _databaseName;
			}
		}

		private void LoadItemPath()
		{
			var itemPath = PathUtils.MakeItemPath(ProviderId);

			// this will result in a path that includes the db as its first node, e.g.
			// /master/sitecore/content/Home
			// we can use an ItemReference to extract this info.

			var reference = ItemReference.Parse(itemPath);

			_itemPath = reference.Path;
			_databaseName = reference.Database;
		}

		public string ProviderId { get; private set; }
	}
}
