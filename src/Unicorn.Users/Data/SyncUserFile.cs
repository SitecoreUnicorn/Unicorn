using Sitecore.Security.Serialization.ObjectModel;

namespace Unicorn.Users.Data
{
	public class SyncUserFile
	{
		public SyncUserFile(SyncUser user, string physicalPath)
		{
			User = user;
			PhysicalPath = physicalPath;
		}

		public SyncUser User { get; }
		public string PhysicalPath { get; }
	}
}
