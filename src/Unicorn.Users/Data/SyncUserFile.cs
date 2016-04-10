namespace Unicorn.Users.Data
{
  using Sitecore.Security.Serialization.ObjectModel;

  public class SyncUserFile
  {
    public SyncUserFile(SyncUser user, string physicalPath)
    {
      this.User = user;
      this.PhysicalPath = physicalPath;
    }

    public SyncUser User { get; }
    public string PhysicalPath { get; }
  }
}
