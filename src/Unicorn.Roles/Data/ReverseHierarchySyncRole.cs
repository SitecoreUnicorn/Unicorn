namespace Unicorn.Roles.Data
{
  using Sitecore.Data.Serialization.ObjectModel;
  using Unicorn.Roles.Model;

  class ReverseHierarchySyncRole : ISyncRole
  {
    public ReverseHierarchySyncRole(SyncRole role)
    {
      this.InnerSyncRole = role;
    }
    public SyncRole InnerSyncRole { get; set; }

    public void ReadRole(Tokenizer reader)
    {
    }

    public string Name => this.InnerSyncRole.Name;
  }
}
