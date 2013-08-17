using System.Collections.Generic;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Data;

namespace Unicorn.Serialization
{
	public class SitecoreSerializedItem : ISerializedItem
	{
		public SitecoreSerializedItem(SyncItem item, string physicalPath)
		{
			InnerItem = item;
			ProviderId = physicalPath;
		}

		public SyncItem InnerItem { get; private set; }

		public ID Id
		{
			get { return LoadIdFromString(InnerItem.ID); }
		}

		public ID ParentId
		{
			get { return LoadIdFromString(InnerItem.ParentID); }
		}

		public string Name
		{
			get { return InnerItem.Name; }
		}

		public ID BranchId
		{
			get { return LoadIdFromString(InnerItem.BranchId); }
		}

		public ID TemplateId
		{
			get { return LoadIdFromString(InnerItem.TemplateID); }
		}

		public string TemplateName
		{
			get { return InnerItem.TemplateName; }
		}

		public string ItemPath
		{
			get { return InnerItem.ItemPath; }
		}

		public string DatabaseName
		{
			get { return InnerItem.DatabaseName; }
		}

		public string ProviderId { get; private set; }


		public SerializedFieldDictionary SharedFields
		{
			get
			{
				var result = new SerializedFieldDictionary();

				foreach (var field in InnerItem.SharedFields)
				{
					result.Add(field.FieldID, field.FieldValue);
				}

				return result;
			}
		}

		public SerializedVersion[] Versions
		{
			get
			{
				var result = new List<SerializedVersion>();
				foreach (var version in InnerItem.Versions)
				{
					var sVersion = new SerializedVersion(version.Language, int.Parse(version.Version));
					foreach (var field in version.Fields)
					{
						sVersion.Fields.Add(field.FieldID, field.FieldValue);
					}
				}

				return result.ToArray();
			}
		}

		private ID LoadIdFromString(string stringId)
		{
			ID id;
			if (!ID.TryParse(stringId, out id)) return (ID)null;

			return id;
		}
	}
}
