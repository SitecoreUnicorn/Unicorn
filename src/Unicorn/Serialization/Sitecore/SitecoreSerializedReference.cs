using System.Diagnostics;
using Sitecore.Data.Serialization;

namespace Unicorn.Serialization.Sitecore
{
	[DebuggerDisplay("SerRef: {ItemPath}")]
	public class SitecoreSerializedReference : ISerializedReference
	{
		private readonly SitecoreSerializationProvider _sourceProvider;

		public SitecoreSerializedReference(string physicalPath, SitecoreSerializationProvider sourceProvider)
		{
			_sourceProvider = sourceProvider;
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

		public string DisplayIdentifier
		{
			get { return DatabaseName + ":" + ItemPath; }
		}

		public ISerializedItem GetItem()
		{
			return _sourceProvider.GetItem(this);
		}

		public ISerializedReference[] GetChildReferences(bool recursive)
		{
			return _sourceProvider.GetChildReferences(this, recursive);
		}

		public ISerializedItem[] GetChildItems()
		{
			return _sourceProvider.GetChildItems(this);
		}

		public void Delete()
		{
			_sourceProvider.DeleteSerializedItem(this);
		}

		private void LoadItemPath()
		{
			var itemPath = PathUtils.MakeItemPath(ProviderId, _sourceProvider.SerializationRoot);

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
