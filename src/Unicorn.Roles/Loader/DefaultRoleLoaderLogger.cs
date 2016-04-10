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

		public void AddedNewRole(IRoleData role)
		{
			_baseLogger.Info($"[A] Role {role.RoleName}");
		}

		public void AddedNewParentRole(IRoleData role)
		{
			_baseLogger.Info($"> [A] Nonexistent parent role {role.RoleName}");
		}

		public void RolesInRolesChanged(IRoleData role, string[] updatedParentRoles, string[] removedParentRoles)
		{
			_baseLogger.Info($"[U] Role {role.RoleName}");
			foreach (var addedRole in updatedParentRoles)
			{
				_baseLogger.Debug($"* [A] Added membership in {addedRole}");
			}
			foreach (var removedRole in removedParentRoles)
			{
				_baseLogger.Debug($"* [D] Removed membership in {removedRole}");
			}
		}
	}
}
