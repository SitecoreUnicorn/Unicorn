using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicorn.Roles.Serealizer
{
  using System.IO;
  using Sitecore.Data.Serialization;
  using Sitecore.Security.Accounts;
  using Sitecore.Shell.Framework.Commands.Serialization;

  class RoleSerializer
  {
    public void DumpRole(string path, Role role)
    {
      Manager.DumpRole(path, role);
      this.AppendParrentRoles(path, role);
    }

    protected void AppendParrentRoles(string path, Role role)
    {
      var roleFile = new FileInfo(path);
      var writer = roleFile.AppendText();
      writer.WriteLine("----parent-roles----");
      var parentRoles = Sitecore.Security.Accounts.RolesInRolesManager.GetRolesForRole(role, false);
      foreach (var parentRole in parentRoles)
      {
        var roleName = $"rolename: {parentRole.Name}";
        writer.WriteLine(roleName);
      }
      writer.Close();

    }
  }
}
