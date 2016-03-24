namespace Unicorn.Roles.Loader
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Web.Security;
  using Sitecore.Diagnostics;
  using Sitecore.Security.Accounts;
  using Unicorn.Configuration;
  using Unicorn.Roles.Data;
  using Unicorn.Roles.RolePredicates;

  class ReverseHierarchyRoleLoader : IRoleLoader
  {
    private readonly IRolePredicate _rolePredicate;
    private readonly IRoleDataStore _roleDataStore;
    private readonly IRoleLoaderLogger _loaderLogger;

    public ReverseHierarchyRoleLoader(IRolePredicate rolePredicate, IRoleDataStore roleDataStore, IRoleLoaderLogger loaderLogger)
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
          DeserializeRole(role);
        }
      }
    }

    private void DeserializeRole(SyncRoleFile role)
    {
      var name = role.Role.Name;
      if (!Roles.RoleExists(name))
      {
        Roles.CreateRole(name);
      }

      Role targetRole = Role.FromName(name);
      var currentParents = this.GetCurrentParents(targetRole);
      var syncRole = role.Role as ReverseHierarchySyncRole;
      if (syncRole != null)
      {
        var innerSyncRole = syncRole.InnerSyncRole;

        foreach (var parentRoleName in innerSyncRole.ParentRoles)
        {
          if (!Role.Exists(parentRoleName))
          {
            Roles.CreateRole(parentRoleName);
          }

          var parentRole = Role.FromName(parentRoleName);
          RolesInRolesManager.AddRoleToRole(targetRole, parentRole);
        }

        var parentsToRemove = currentParents.Where(parent => !innerSyncRole.ParentRoles.Contains(parent.Name));
        foreach (var parentToRemove in parentsToRemove)
        {
          RolesInRolesManager.RemoveRoleFromRole(targetRole, parentToRemove);
        }
      }
    }

    private IEnumerable<Role> GetCurrentParents(Role role)
    {
      return RolesInRolesManager.GetRolesForRole(role, false);
    } 
  }
}

