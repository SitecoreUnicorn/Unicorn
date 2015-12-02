using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using System.Globalization;
using Rainbow.Storage.Sc;
using Sitecore.Data.Items;

namespace Unicorn.Data.DataProvider
{
	/// <summary>
	/// Uses ItemChanges to detect fields being removed on item save.
	/// This gets around the fact that when Sitecore resets a field to standard values, the field
	/// is still listed as 'IsStandardValue = false' during the save, but the field is marked as 'RemoveField' in the changes.
	/// 
	/// This allows Unicorn to signal to Rainbow that it should remove fields being reset to standard values from serialized copies.
	/// </summary>
	public class ItemChangesFilteredItemData : ItemData
	{
		private readonly ItemChanges _changes;

		public ItemChangesFilteredItemData(ItemChanges changes) : base(changes.Item)
		{
			_changes = changes;
		}

		public override IEnumerable<IItemFieldValue> SharedFields
		{
			get
			{
				return base.SharedFields.Where(field =>
				{
					var fieldChange = _changes.FieldChanges[new Sitecore.Data.ID(field.FieldId)];

					if (fieldChange == null || !fieldChange.RemoveField) return true;

					return false;
				});
			}
		}

		public override IEnumerable<IItemVersion> Versions
		{
			get { return base.Versions.Select(version => new ItemChangesFilteredItemVersion(version, _changes)); }
		}

		protected class ItemChangesFilteredItemVersion : IItemVersion
		{
			private readonly IItemVersion _innerVersion;
			private readonly ItemChanges _changes;

			public ItemChangesFilteredItemVersion(IItemVersion innerVersion, ItemChanges changes)
			{
				_innerVersion = innerVersion;
				_changes = changes;
			}

			public IEnumerable<IItemFieldValue> Fields
			{
				get
				{
					return _innerVersion.Fields.Where(field =>
					{
						var fieldChange = _changes.FieldChanges[new Sitecore.Data.ID(field.FieldId)];

						if (fieldChange == null || !fieldChange.RemoveField) return true;

						return false;
					});
				}
			}

			public CultureInfo Language { get { return _innerVersion.Language; } }

			public int VersionNumber { get { return _innerVersion.VersionNumber; } }
		}
	}
}
