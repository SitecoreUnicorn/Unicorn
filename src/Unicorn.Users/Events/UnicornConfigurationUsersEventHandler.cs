namespace Unicorn.Users.Events
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
  using Configuration;
  using Sitecore.Diagnostics;
  using Unicorn.Users.Data;
  using Unicorn.Users.Predicates;

  class UnicornConfigurationUsersEventHandler
  {
    private readonly IUserPredicate _predicate;
    private readonly IUserDataStore _dataStore;
    private IConfiguration config;

    public UnicornConfigurationUsersEventHandler(IConfiguration configuration)
    {
      Assert.ArgumentNotNull(configuration, nameof(configuration));

      _predicate = configuration.Resolve<IUserPredicate>();
      _dataStore = configuration.Resolve<IUserDataStore>();
    }
  }
}
