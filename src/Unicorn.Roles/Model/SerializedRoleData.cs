using Sitecore.Diagnostics;
using Unicorn.Roles.Data;

namespace Unicorn.Roles.Model
{
	/// <summary>
	/// Wraps a Sitecore SyncRole object along with the source filename it's from
	/// </summary>
	public class SerializedRoleData : IRoleData
	{
		public SerializedRoleData(string roleName, string[] parentRoleNames, string serializedItemId)
		{
			Assert.ArgumentNotNullOrEmpty(roleName, nameof(roleName));
			Assert.ArgumentNotNull(parentRoleNames, nameof(parentRoleNames));
			Assert.ArgumentNotNullOrEmpty(serializedItemId, nameof(serializedItemId));

			RoleName = roleName;
			ParentRoleNames = parentRoleNames;
			SerializedItemId = serializedItemId;
		}

		public string RoleName { get; }
		public string[] ParentRoleNames { get; }
		public string SerializedItemId { get; }
	}
}
