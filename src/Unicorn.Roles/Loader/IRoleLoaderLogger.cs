using Unicorn.Roles.Model;

namespace Unicorn.Roles.Loader
{
	public interface IRoleLoaderLogger
	{
		void AddedNewRole(IRoleData role);

		void AddedNewRoleMembership(IRoleData role);

		void RoleMembershipChanged(IRoleData role, string[] updatedRoleMembership, string[] removedRoleMembership);
		void RemovedOrphanRole(IRoleData orphan);
	}
}
