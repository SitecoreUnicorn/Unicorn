using Unicorn.Logging;
using Unicorn.Roles.Model;

namespace Unicorn.Roles.Loader
{
	/// <summary>
	/// Handles logging for the role loader
	/// </summary>
	public class DefaultRoleLoaderLogger : IRoleLoaderLogger
	{
		private readonly ILogger _baseLogger;

		public DefaultRoleLoaderLogger(ILogger baseLogger)
		{
			_baseLogger = baseLogger;
		}

		public virtual void AddedNewRole(IRoleData role)
		{
			_baseLogger.Info($"[A] Role {role.RoleName}");
		}

		public virtual void AddedNewRoleMembership(IRoleData role)
		{
			_baseLogger.Info($"> [A] Nonexistent parent role {role.RoleName}");
		}

		public virtual void RoleMembershipChanged(IRoleData role, string[] updatedRoleMembership, string[] removedRoleMembership)
		{
			_baseLogger.Info($"[U] Role {role.RoleName}");
			foreach (var addedRole in updatedRoleMembership)
			{
				_baseLogger.Debug($"* [A] Added membership in {addedRole}");
			}
			foreach (var removedRole in removedRoleMembership)
			{
				_baseLogger.Debug($"* [D] Removed membership in {removedRole}");
			}
		}

		public virtual void RemovedOrphanRole(IRoleData orphan)
		{
			_baseLogger.Warn($"[D] Deleted role {orphan.RoleName} because it was not in the serialization store. If deleted by mistake, you can recreate the role by name to restore it.");
		}
	}
}
