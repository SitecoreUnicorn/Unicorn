namespace Unicorn.Users.Events
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
  using System.Web.Security;
  using Sitecore.Data.Events;
  using Sitecore.Diagnostics;
  using Sitecore.Events;
  using Unicorn.Configuration;

  class UnicornUsersEventHandler
  {
    private readonly UnicornConfigurationUsersEventHandler[] _configurations;

    public UnicornUsersEventHandler(): this(UnicornConfigurationManager.Configurations)
		{

    }

    protected UnicornUsersEventHandler(IEnumerable<IConfiguration> configurations)
    {
      _configurations = configurations.Select(config => new UnicornConfigurationUsersEventHandler(config)).ToArray();
    }

    public virtual void UserCreated(object sender, EventArgs e)
    {
      if (EventDisabler.IsActive)
      {
        return;
      }

      var user = Event.ExtractParameter<MembershipUser>(e, 0);

      Assert.IsNotNullOrEmpty(user.UserName, "User name was null or empty!");

      foreach (var configuration in _configurations)
      {
        configuration.UserAlteredOrCreated(user.UserName);
      }
    }

    public virtual void UserRemoved(object sender, EventArgs e)
    {
      if (EventDisabler.IsActive)
      {
        return;
      }

      string userName = Event.ExtractParameter<string>(e, 0);

      Assert.IsNotNullOrEmpty(userName, "User name was null or empty!");

      foreach (var configuration in _configurations)
      {
        configuration.UserDeleted(userName);
      }
    }

    public virtual void RoleReferenceUpdated(object sender, EventArgs e)
    {
      if (EventDisabler.IsActive)
      {
        return;
      }

      var parameter = Event.ExtractParameter<object>(e, 0);

      var userName = (parameter as string[])[0];
      Assert.IsNotNullOrEmpty(userName, "User name was null or empty!");

      foreach (var configuration in _configurations)
      {
        configuration.UserAlteredOrCreated(userName);
      }
    }


    public virtual void UserUpdated(object sender, EventArgs e)
    {
      if (EventDisabler.IsActive)
      {
        return;
      }

      var user = Event.ExtractParameter<MembershipUser>(e, 0);

      Assert.IsNotNullOrEmpty(user.UserName, "User name was null or empty!");

      foreach (var configuration in _configurations)
      {
        configuration.UserAlteredOrCreated(user.UserName);
      }
    }

  }
}
