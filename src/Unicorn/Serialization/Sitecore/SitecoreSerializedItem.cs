using System.Linq;
using System.Collections.Generic;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Data;
using System.Diagnostics;
using Unicorn.Data;

namespace Unicorn.Serialization.Sitecore
{
	[DebuggerDisplay("SerItem: {ItemPath}")]
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

		public string DisplayIdentifier
		{
			get { return string.Format("{0}:{1} ({2})", DatabaseName, ItemPath, Id); }
		}

		public FieldDictionary SharedFields
		{
			get
			{
				var result = new FieldDictionary();

				foreach (var field in InnerItem.SharedFields)
				{
					result.Add(field.FieldID, field.FieldValue);
				}

				return result;
			}
		}

		public ItemVersion[] Versions
		{
			get
			{
				var result = new List<ItemVersion>();
				foreach (var version in InnerItem.Versions)
				{
					var sVersion = new ItemVersion(version.Language, int.Parse(version.Version));
					foreach (var field in version.Fields)
					{
						sVersion.Fields.Add(field.FieldID, field.FieldValue);
					}

					result.Add(sVersion);
				}

				return result.ToArray();
			}
		}

		private ID LoadIdFromString(string stringId)
		{
			ID id;
			if (!ID.TryParse(stringId, out id)) return null;

			return id;
		}
	}
}
