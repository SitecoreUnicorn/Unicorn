namespace Unicorn.Users.Events
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
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

  }
}
