using Unicorn.Roles.Data;
using Unicorn.Roles.Model;

namespace Unicorn.Roles.Loader
{
	public interface IRoleLoaderLogger
	{
		void AddedNewRole(IRoleData role);

		void AddedNewParentRole(IRoleData role);

		void RolesInRolesChanged(IRoleData role, string[] updatedParentRoles, string[] removedParentRoles);
	}
}
