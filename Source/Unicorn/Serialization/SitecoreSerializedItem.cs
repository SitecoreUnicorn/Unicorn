using System;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Data;

namespace Unicorn.Serialization
{
	public class SitecoreSerializedItem : ISerializedItem
	{
		public SitecoreSerializedItem(SyncItem item)
		{
				
		}

		public ID Id
		{
			get { throw new NotImplementedException(); }
		}

		public ID ParentId
		{
			get { throw new NotImplementedException(); }
		}

		public string Name
		{
			get { throw new NotImplementedException(); }
		}

		public ID BranchId
		{
			get { throw new NotImplementedException(); }
		}

		public ID TemplateId
		{
			get { throw new NotImplementedException(); }
		}

		public string TemplateName
		{
			get { throw new NotImplementedException(); }
		}

		public string ItemPath
		{
			get { throw new NotImplementedException(); }
		}

		public string DatabaseName
		{
			get { throw new NotImplementedException(); }
		}

		public string ProviderId
		{
			get { throw new NotImplementedException(); }
		}
	}
}
