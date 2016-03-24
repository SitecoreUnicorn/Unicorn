namespace Unicorn.Roles.Data
{
  using Sitecore.Data.Serialization.ObjectModel;

  public interface ISyncRole
  {
    void ReadRole(Tokenizer reader);

    string Name { get;}
  }
}
