namespace Unicorn.Users.Events
{
  using Sitecore.Diagnostics;
  using Sitecore.Security.Accounts;
  using Unicorn.Configuration;
  using Unicorn.Users.Data;
  using Unicorn.Users.Predicates;

  class UnicornConfigurationUsersEventHandler
  {
    private readonly IUserPredicate _predicate;
    private readonly IUserDataStore _dataStore;

    public UnicornConfigurationUsersEventHandler(IConfiguration configuration)
    {
      Assert.ArgumentNotNull(configuration, nameof(configuration));

      this._predicate = configuration.Resolve<IUserPredicate>();
      this._dataStore = configuration.Resolve<IUserDataStore>();
    }


    public virtual void UserAlteredOrCreated(string userName)
    {
      var user = User.FromName(userName, false);
      if (this._predicate == null || !this._predicate.Includes(user).IsIncluded)
      {
        return;
      }

      this._dataStore.Save(user);
    }

    public void RoleDeleted(string userName)
    {
      var user = User.FromName(userName, false);

      if (this._predicate == null || !this._predicate.Includes(user).IsIncluded) return;

      this._dataStore.Remove(user);
    }
  }
}
