namespace Unicorn.Roles.Data
{
  using Sitecore.Data.Serialization.ObjectModel;
  using Sitecore.Security.Serialization.ObjectModel;

  class DefaultSyncRole : ISyncRole
  {
    public SyncRole InnerSyncRole { get; set; }

    public void ReadRole(Tokenizer reader)
    {
    }

    public string Name => this.InnerSyncRole.Name;
  }
}
