using System;
using System.Collections.Generic;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Sc.Deserialization;

namespace Unicorn.Data.Dilithium
{
	public class DilithiumSitecoreDataStore : SitecoreDataStore
	{

		public DilithiumSitecoreDataStore(IDeserializer deserializer) : base(deserializer)
		{
			
		}

		public override string FriendlyName => "Dilithium Sitecore Data Store";
		public override string Description => "Reads and writes data from a Sitecore database. Reads are performed with direct SQL batches for extreme speed.";

		public override void Save(IItemData item)
		{
			if (ReactorContext.SqlPrecache != null) ReactorContext.SqlPrecache.Update(item);

			base.Save(item);
		}

		public override bool Remove(IItemData item)
		{
			if (ReactorContext.SqlPrecache != null) ReactorContext.SqlPrecache.Remove(item);

			return base.Remove(item);
		}

		public override IItemData GetById(Guid id, string database)
		{
			if (ReactorContext.SqlPrecache != null) return ReactorContext.SqlPrecache.GetById(id, database);

			return base.GetById(id, database);
		}

		public override IEnumerable<IItemData> GetByPath(string path, string database)
		{
			if (ReactorContext.SqlPrecache != null) return ReactorContext.SqlPrecache.GetByPath(path, database);

			return base.GetByPath(path, database);
		}

		public override IItemData GetByPathAndId(string path, Guid id, string database)
		{
			return GetById(id, database);
		}

		public override IEnumerable<IItemData> GetChildren(IItemData parentItem)
		{
			if (ReactorContext.SqlPrecache != null) return ReactorContext.SqlPrecache.GetChildren(parentItem);

			return base.GetChildren(parentItem);
		}
	}
}
