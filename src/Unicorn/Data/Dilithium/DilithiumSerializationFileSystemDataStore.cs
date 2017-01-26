using System;
using System.Collections.Generic;
using Rainbow.Formatting;
using Rainbow.Model;
using Rainbow.Storage;

namespace Unicorn.Data.Dilithium
{
	public class DilithiumSerializationFileSystemDataStore : SerializationFileSystemDataStore
	{
		public DilithiumSerializationFileSystemDataStore(string physicalRootPath, bool useDataCache, ITreeRootFactory rootFactory, ISerializationFormatter formatter) : base(physicalRootPath, useDataCache, rootFactory, formatter)
		{
		}

		public override string FriendlyName => "Dilithium Serialization File System Data Store";
		public override string Description => "Stores serialized items on disk using the SFS tree format, where each root is a separate tree. Supports super fast batch reads for high speed syncing with Dilithium.";

		public override IItemData GetById(Guid id, string database)
		{
			if (ReactorContext.RainbowPrecache != null) return ReactorContext.RainbowPrecache.GetById(id, database);

			return base.GetById(id, database);
		}

		public override IEnumerable<IItemData> GetByPath(string path, string database)
		{
			if (ReactorContext.RainbowPrecache != null) return ReactorContext.RainbowPrecache.GetByPath(path, database);

			return base.GetByPath(path, database);
		}

		public override IItemData GetByPathAndId(string path, Guid id, string database)
		{
			return GetById(id, database);
		}

		public override IEnumerable<IItemData> GetChildren(IItemData parentItem)
		{
			if (ReactorContext.RainbowPrecache != null) return ReactorContext.RainbowPrecache.GetChildren(parentItem);

			return base.GetChildren(parentItem);
		}

		
	}
}
