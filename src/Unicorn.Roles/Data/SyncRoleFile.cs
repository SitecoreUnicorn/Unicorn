using Sitecore.Security.Serialization.ObjectModel;

namespace Unicorn.Roles.Data
{
	public class SyncRoleFile
	{
		public SyncRoleFile(SyncRole role, string physicalPath)
		{
			Role = role;
			PhysicalPath = physicalPath;
		}

		public SyncRole Role { get; }
		public string PhysicalPath { get; }
	}
}
