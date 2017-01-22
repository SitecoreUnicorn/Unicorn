using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rainbow.Model;

namespace Unicorn.Data.Dilithium.Data
{
	[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [DILITHIUM]")]
	public class DilithiumItemData : IItemData
	{
		private readonly DataCore _sourceDataCore;

		public DilithiumItemData(DataCore sourceDataCore)
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
		public IList<DilithiumFieldValue> RawSharedFields { get; } = new List<DilithiumFieldValue>();
		public IList<DilithiumItemLanguage> RawUnversionedFields { get; } = new List<DilithiumItemLanguage>(); 
		public IList<DilithiumItemVersion> RawVersions { get; } = new List<DilithiumItemVersion>(); 
	}
}
