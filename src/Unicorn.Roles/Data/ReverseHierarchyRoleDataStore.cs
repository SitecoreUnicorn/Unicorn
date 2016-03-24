namespace Unicorn.Roles.Data
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
  using Sitecore.Data.Serialization.ObjectModel;
  using Sitecore.Diagnostics;
  using Sitecore.Security.Accounts;
  using Unicorn.Roles.Model;

  public class ReverseHierarchyRoleDataStore :SerializedRoleDataStore
  {
    public ReverseHierarchyRoleDataStore(string physicalRootPath) : base(physicalRootPath)
    {
    }

    public override void Save(Role role)
    {
      var path = this.GetPathForRole(role);
      var syncRole = SyncRole.Create(role);
      syncRole.Serialize(path);
    }

    protected override SyncRoleFile ReadSyncRole(string path)
    {
      Assert.ArgumentNotNullOrEmpty(path, nameof(path));

      using (TextReader reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
      {
        var syncRole = new ReverseHierarchySyncRole(SyncRole.ReadRole(new Tokenizer(reader)));
        return new SyncRoleFile(syncRole, path);
      }
    }
  }
}
