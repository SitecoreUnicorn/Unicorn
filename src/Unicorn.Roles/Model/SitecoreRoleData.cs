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
		public string[] ParentRoleNames => RolesInRolesManager.GetRolesForRole(_role, false).Select(parentRole => parentRole.Name).ToArray();
		public string SerializedItemId => "[DB ROLE]";
	}
}
