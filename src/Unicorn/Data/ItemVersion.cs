using System;
using Sitecore;

namespace Unicorn.Data
{
	/// <summary>
	/// Represents a given version of an item. Note that this is used both by ISourceItem AND ISerializedItem.
	/// </summary>
	public class ItemVersion
	{
		public ItemVersion(string language, int versionNumber)
		{
			Language = language;
			VersionNumber = versionNumber;
			Fields = new FieldDictionary();
		}

		public int VersionNumber { get; private set; }
		public string Language { get; private set; }

		public DateTime? Updated
		{
			get
			{
				string fieldValue;

				if (!Fields.TryGetValue(FieldIDs.Updated.ToString(), out fieldValue)) return null;
				if (fieldValue == null) return null;

				if (DateUtil.IsIsoDate(fieldValue))
					return DateUtil.IsoDateToDateTime(fieldValue);

				return null;
			}
		}

		public string Revision
		{
			get
			{
				string fieldValue;

				if (!Fields.TryGetValue(FieldIDs.Revision.ToString(), out fieldValue)) return null;
				if (fieldValue == null) return null;

				return fieldValue;
			}
		}

		public FieldDictionary Fields { get; private set; }
	}
}
