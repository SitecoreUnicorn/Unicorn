using System;
using Sitecore;

namespace Unicorn.Serialization
{
	public class SerializedVersion
	{
		public SerializedVersion(string language, int versionNumber)
		{
			Language = language;
			VersionNumber = versionNumber;
			Fields = new SerializedFieldDictionary();
		}

		public int VersionNumber { get; private set; }
		public string Language { get; private set; }

		public DateTime? Updated
		{
			get
			{
				var fieldValue = Fields[FieldIDs.Updated.ToString()];
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
				var fieldValue = Fields[FieldIDs.Revision.ToString()];
				if (fieldValue == null) return null;

				return fieldValue;
			}
		}

		public SerializedFieldDictionary Fields { get; private set; }
	}
}
