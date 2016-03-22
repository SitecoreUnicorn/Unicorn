namespace Unicorn.Roles.Data
{
  using Unicorn.Roles.Model;

  /// <summary>
	/// Wraps a Sitecore SyncRole object along with the source filename it's from
	/// </summary>
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
