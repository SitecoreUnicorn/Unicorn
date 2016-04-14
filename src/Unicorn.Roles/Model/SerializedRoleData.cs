using Sitecore.Diagnostics;
using Unicorn.Roles.Data;

namespace Unicorn.Roles.Model
{
	/// <summary>
	/// Wraps a Sitecore SyncRole object along with the source filename it's from
	/// </summary>
	public class SerializedRoleData : IRoleData
	{
		public SerializedRoleData(string roleName, string[] memberOfRoles, string serializedItemId)
		{
			Assert.ArgumentNotNullOrEmpty(roleName, nameof(roleName));
			Assert.ArgumentNotNull(memberOfRoles, nameof(memberOfRoles));
			Assert.ArgumentNotNullOrEmpty(serializedItemId, nameof(serializedItemId));

			RoleName = roleName;
			MemberOfRoles = memberOfRoles;
			SerializedItemId = serializedItemId;
		}

		public string RoleName { get; }
		public string[] MemberOfRoles { get; }
		public string SerializedItemId { get; }
	}
}
