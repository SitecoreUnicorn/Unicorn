namespace Unicorn.Users.Data
{
  using System.Collections.Generic;
  using Sitecore.Security.Accounts;

  public interface IUserDataStore
  {
    IEnumerable<SyncUserFile> GetAll();

    void Save(User user);

    /// <summary>
    /// Removes a role from the data store, if it exists.
    /// If it does not exist, does nothing.
    /// </summary>
    void Remove(User user);

    /// <summary>
    /// Removes all roles from the data store.
    /// </summary>
    void Clear();
  }
}
