using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rainbow.Model;

namespace Unicorn.Data.Dilithium.Rainbow
{
	[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [DILITHIUM RAINBOW]")]
	public class RainbowItemData : IItemData
	{
		private readonly IItemData _baseItemData;
		private readonly RainbowDataCache _sourceDataCore;

		public RainbowItemData(IItemData baseItemData, RainbowDataCache sourceDataCore)
		{
			_baseItemData = baseItemData;
			_sourceDataCore = sourceDataCore;
		}

		public Guid Id => _baseItemData.Id;

		public Guid ParentId => _baseItemData.ParentId;

		public Guid TemplateId => _baseItemData.TemplateId;

		public string Path => _baseItemData.Path;

		public string SerializedItemId => _baseItemData.SerializedItemId;

		public string DatabaseName
		{
			get { return _baseItemData.DatabaseName; }
			set { _baseItemData.DatabaseName = value; }
		}

		public string Name => _baseItemData.Name;

		public Guid BranchId => _baseItemData.BranchId;

		public IEnumerable<IItemFieldValue> SharedFields => _baseItemData.SharedFields;

		public IEnumerable<IItemLanguage> UnversionedFields => _baseItemData.UnversionedFields;

		public IEnumerable<IItemVersion> Versions => _baseItemData.Versions;

		public IEnumerable<IItemData> GetChildren()
		{
			return _sourceDataCore.GetChildren(this);
		}

		// Dilithium extensions
		public IList<Guid> Children { get; } = new List<Guid>();
	}
}
