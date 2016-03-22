namespace Unicorn.Users.Data
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
  using Sitecore.Security.Serialization.ObjectModel;

  class SyncUserFile
  {
    public SyncUserFile(SyncUser user, string physicalPath)
    {
      User = user;
      PhysicalPath = physicalPath;
    }

    public SyncUser User { get; }
    public string PhysicalPath { get; }
  }
}
