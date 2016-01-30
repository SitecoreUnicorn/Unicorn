using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization;
using Unicorn.Configuration;
using Unicorn.Roles.Data;
using Unicorn.Roles.RolePredicates;

namespace Unicorn.Roles.Loader
{
	public class RoleLoader : IRoleLoader
	{
		private readonly IRolePredicate _rolePredicate;
		private readonly IRoleDataStore _roleDataStore;
		private readonly IRoleLoaderLogger _loaderLogger;

		public RoleLoader(IRolePredicate rolePredicate, IRoleDataStore roleDataStore, IRoleLoaderLogger loaderLogger)
		{
			Assert.ArgumentNotNull(rolePredicate, nameof(rolePredicate));
			Assert.ArgumentNotNull(roleDataStore, nameof(roleDataStore));
			Assert.ArgumentNotNull(loaderLogger, nameof(loaderLogger));

			_rolePredicate = rolePredicate;
			_roleDataStore = roleDataStore;
			_loaderLogger = loaderLogger;
		}

		public void Load(IConfiguration configuration)
		{
			using (new UnicornOperationContext())
			{
				var roles = _roleDataStore
					.GetAll()
					.Where(role => _rolePredicate.Includes(Role.FromName(role.Role.Name)).IsIncluded);

				foreach (var role in roles)
				{
					if (EvaluateRole(role))
					{
						DeserializeRole(role);
					}
				}
			}
		}

		protected virtual bool EvaluateRole(SyncRoleFile role)
		{
			if (!Role.Exists(role.Role.Name))
			{
				_loaderLogger.EvaluatedNewRole(role);
				return true;
			}

			var targetRole = Role.FromName(role.Role.Name);

			var rolesInRole = RolesInRolesManager.GetRolesInRole(targetRole, false).ToLookup(key => key.Name);

			var addedRolesInRoles = role.Role.RolesInRole.Where(rir => !rolesInRole.Contains(rir)).ToArray();

			var removedRolesInRoles = rolesInRole.Where(rir => !role.Role.RolesInRole.Contains(rir.Key)).Select(rir => rir.Key).ToArray();

			if (addedRolesInRoles.Length > 0 || removedRolesInRoles.Length > 0)
			{
				_loaderLogger.RolesInRolesChanged(role, addedRolesInRoles, removedRolesInRoles);
				return true;
			}

			return false;
		}

		protected virtual void DeserializeRole(SyncRoleFile role)
		{
			RoleSynchronization.PasteSyncRole(role.Role);
		}
	}
}
