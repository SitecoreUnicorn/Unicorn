using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rainbow.Model;

namespace Unicorn.Data.Dilithium.Sql
{
	[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [DILITHIUM SQL]")]
	public class SqlItemData : IItemData
	{
		private readonly SqlDataCache _sourceDataCore;

		public SqlItemData(SqlDataCache sourceDataCore)
		{
			_sourceDataCore = sourceDataCore;
		}

		public Guid Id { get; set; }

		public Guid ParentId { get; set; }

		public Guid TemplateId { get; set; }

		public string Path { get; set; }

		public string SerializedItemId => "(from Sitecore Database [Dilithium])";

		public string DatabaseName { get; set; }

		public string Name { get; set; }

		public Guid BranchId { get; set; }

		public IEnumerable<IItemFieldValue> SharedFields => RawSharedFields;

		public IEnumerable<IItemLanguage> UnversionedFields => RawUnversionedFields;

		public IEnumerable<IItemVersion> Versions => RawVersions;

		public IEnumerable<IItemData> GetChildren()
		{
			return _sourceDataCore.GetChildren(this);
		}

		// Dilithium extensions
		public IList<Guid> Children { get; } = new List<Guid>();
		// the predicate tree root item ID this item was sourced for
		public IList<SqlItemFieldValue> RawSharedFields { get; } = new List<SqlItemFieldValue>();
		public IList<SqlItemLanguage> RawUnversionedFields { get; } = new List<SqlItemLanguage>(); 
		public IList<SqlItemVersion> RawVersions { get; } = new List<SqlItemVersion>(); 
	}
}
