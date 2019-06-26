using System.Linq;
using Sitecore.Security.Accounts;

namespace Unicorn.Roles.Model
{
	public class SitecoreRoleData : IRoleData
	{
		private readonly Role _role;

		public SitecoreRoleData(Role role)
		{
			_role = role;
		}

		public string RoleName => _role.Name;
		public string[] MemberOfRoles => RolesInRolesManager.GetRolesForRole(_role, false).Select(memberRole => memberRole.Name).ToArray();
		public string SerializedItemId => "[DB ROLE]";
	}
}
