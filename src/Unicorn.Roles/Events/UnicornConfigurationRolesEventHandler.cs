using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Unicorn.Configuration;
using Unicorn.Roles.Data;
using Unicorn.Roles.RolePredicates;

namespace Unicorn.Roles.Events
{
  using System.Linq;

  /// <summary>
	/// Handles role change events for a specific Unicorn configuration
	/// </summary>
	public class UnicornConfigurationRolesEventHandler
	{
		private readonly IRolePredicate _predicate;
		private readonly IRoleDataStore _dataStore;

		public UnicornConfigurationRolesEventHandler(IConfiguration configuration)
		{
			Assert.ArgumentNotNull(configuration, nameof(configuration));

			_predicate = configuration.Resolve<IRolePredicate>();
			_dataStore = configuration.Resolve<IRoleDataStore>();
		}

		public virtual void RoleAlteredOrCreated(string roleName)
		{
      var role = Role.FromName(roleName);

      if (!Role.Exists(roleName))
      {
        var roles = RolesInRolesManager.GetAllRoles();
        foreach (var role1 in roles)
        {
          if (_predicate == null || !_predicate.Includes(role1).IsIncluded) return;

          _dataStore.Save(role1);
        }
      }
      else
      {
        if (_predicate == null || !_predicate.Includes(role).IsIncluded) return;
        _dataStore.Save(role);
      }
		}

		public virtual void RoleDeleted(string roleName)
		{
			var role = Role.FromName(roleName);

			if (_predicate == null || !_predicate.Includes(role).IsIncluded) return;

			_dataStore.Remove(role);
		}
	}
}
