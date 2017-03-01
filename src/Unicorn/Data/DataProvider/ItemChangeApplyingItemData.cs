using System;
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
	public class ItemChangeApplyingItemData : ItemDecorator
	{
		private readonly ItemChanges _changes;

		public ItemChangeApplyingItemData(IItemData existingItem, ItemChanges changes) : base(existingItem)
		{
			_changes = changes;
		}

		public ItemChangeApplyingItemData(ItemChanges changes) : base(new ProxyItem(changes.Item.Name, changes.Item.ID.Guid, changes.Item.ParentID.Guid, changes.Item.TemplateID.Guid, changes.Item.Paths.Path, changes.Item.Database.Name, () => changes.Item.Children.Select(child => new ItemData(child))))
		{
			_changes = changes;
		}

		public override Guid BranchId
		{
			get
			{
				const string branchProperty = "branchid";
				if (_changes.IsPropertyModified(branchProperty))
					return Guid.Parse(_changes.GetPropertyValue(branchProperty).ToString());

				return base.BranchId;
			}
		}

		public override IEnumerable<IItemFieldValue> SharedFields
		{
			get
			{
				if (!_changes.HasFieldsChanged) return base.SharedFields;

				return new FieldChangeParser().ParseFieldChanges(_changes.FieldChanges.Cast<FieldChange>().Where(FieldChangeHelper.IsShared), base.SharedFields, DatabaseName);
			}
		}

		public override IEnumerable<IItemLanguage> UnversionedFields
		{
			get
			{
				// PHASE 1: Make sure all languages exist that need to
				var baseLanguages = base.UnversionedFields.ToDictionary(language => language.Language.Name);

				// merge with any languages found in the item changes
				foreach (FieldChange field in _changes.FieldChanges)
				{
					// if the base languages does not contain the specified language from the changes, we have to infer its addition
					if (!baseLanguages.ContainsKey(field.Language.Name) && field.Definition != null && field.Definition.IsUnversioned)
					{
						baseLanguages.Add(field.Language.Name, new ProxyItemLanguage(field.Language.CultureInfo));
					}
				}

				// PHASE 2: merge fields between matching languages
				return baseLanguages.Values.Select(language =>
				{
					var targetLanguage = language.Language.Name;
					var targetFieldChanges = _changes.FieldChanges
						.Cast<FieldChange>()
						.Where(change => change.Language.Name.Equals(targetLanguage, StringComparison.Ordinal))
						.Where(FieldChangeHelper.IsUnversioned);

					return new ProxyItemLanguage(language)
					{
						Fields = new FieldChangeParser().ParseFieldChanges(targetFieldChanges, language.Fields, DatabaseName)
					};
				});
			}
		}

		public override IEnumerable<IItemVersion> Versions
		{
			get
			{
				// Inexplicably during package installation Sitecore can actually IMPLICITLY create versions,
				// by calling SaveItem() in the DP and passing in language and version combos that have NOT
				// had the DataProvider's AddVersion() called for them yet. So when adapting to ItemChanges we must infer any nonexistant versions,
				// and create them implicitly as well
				var baseVersions = base.Versions.ToDictionary(version => $"{version.Language.Name}#{version.VersionNumber}");

				foreach (FieldChange field in _changes.FieldChanges)
				{
					string key = $"{field.Language.Name}#{field.Version.Number}";

					// if the base versions does not contain the specified version from the changes, and the field is versioned, we have to infer its addition
					if (!baseVersions.ContainsKey(key) && field.Definition != null && field.Definition.IsVersioned)
					{
						baseVersions.Add(key, new ProxyItemVersion(field.Language.CultureInfo, field.Version.Number));
					}
				}

				return baseVersions.Values.Select(version => new ItemChangeApplyingItemVersion(version, _changes));
			}
		}

		protected class ItemChangeApplyingItemVersion : IItemVersion
		{
			private readonly IItemVersion _innerVersion;
			private readonly ItemChanges _changes;

			public ItemChangeApplyingItemVersion(IItemVersion innerVersion, ItemChanges changes)
			{
				_innerVersion = innerVersion;
				_changes = changes;
			}

			public IEnumerable<IItemFieldValue> Fields
			{
				get
				{
					var targetLanguage = Language.Name;
					var targetFieldChanges = _changes.FieldChanges
						.Cast<FieldChange>()
						.Where(change => change.Language.Name.Equals(targetLanguage, StringComparison.Ordinal) && change.Version.Number.Equals(VersionNumber))
						.Where(FieldChangeHelper.IsVersioned);
					
					return new FieldChangeParser().ParseFieldChanges(targetFieldChanges, _innerVersion.Fields, _changes.Item.Database.Name);
				}
			}

			public CultureInfo Language => _innerVersion.Language;

			public int VersionNumber => _innerVersion.VersionNumber;
		}

		private static class FieldChangeHelper
		{
			public static bool IsShared(FieldChange change)
			{
				var def = change.Definition;
				if (def == null) return false;

				return def.IsShared;
			}

			public static bool IsVersioned(FieldChange change)
			{
				var def = change.Definition;
				if (def == null) return false;

				return def.IsVersioned;
			}

			public static bool IsUnversioned(FieldChange change)
			{
				var def = change.Definition;
				if (def == null) return false;

				return def.IsUnversioned && !def.IsShared;
			}
		}
	}
}
