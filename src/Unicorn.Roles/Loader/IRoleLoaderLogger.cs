using Unicorn.Roles.Data;

namespace Unicorn.Roles.Loader
{
	public interface IRoleLoaderLogger
	{
		void EvaluatedNewRole(SyncRoleFile role);
		void RolesInRolesChanged(SyncRoleFile role, string[] addedRolesInRoles, string[] removedRolesInRoles);
	}
}
