using Unicorn.Logging;
using Unicorn.Roles.Data;

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

		public void EvaluatedNewRole(SyncRoleFile role)
		{
			_baseLogger.Info($"[A] Role {role.Role.Name}");
		}

		public void RolesInRolesChanged(SyncRoleFile role, string[] addedRolesInRoles, string[] removedRolesInRoles)
		{
			_baseLogger.Info($"[U] Role {role.Role.Name}");
			foreach (var addedRole in addedRolesInRoles)
			{
				_baseLogger.Debug($"* [A] Role member {addedRole}");
			}
			foreach (var removedRole in removedRolesInRoles)
			{
				_baseLogger.Debug($"* [D] Role member {removedRole}");
			}
		}
	}
}
