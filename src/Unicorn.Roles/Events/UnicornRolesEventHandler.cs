using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Events;
using Sitecore.Security.Accounts;
using Unicorn.Configuration;

namespace Unicorn.Roles.Events
{
	public class UnicornRolesEventHandler
	{
		private readonly UnicornConfigurationRolesEventHandler[] _configurations;

		public UnicornRolesEventHandler(): this(UnicornConfigurationManager.Configurations)
		{
			
		}

		protected UnicornRolesEventHandler(IEnumerable<IConfiguration> configurations)
		{
			_configurations = configurations.Select(config => new UnicornConfigurationRolesEventHandler(config)).ToArray();
		}

		public void RoleCreated(object sender, EventArgs e)
		{
			string roleName = Event.ExtractParameter<string>(e, 0);

			foreach (var configuration in _configurations)
			{
				configuration.RoleAlteredOrCreated(roleName);
			}
		}

		public void RoleRemoved(object sender, EventArgs e)
		{
			string roleName = Event.ExtractParameter<string>(e, 0);
			foreach (var configuration in _configurations)
			{
				configuration.RoleDeleted(roleName);
			}
		}

		public void RolesInRolesRemoved(object sender, EventArgs e)
		{
			string roleName = Event.ExtractParameter<string>(e, 0);
			foreach (var configuration in _configurations)
			{
				configuration.RoleAlteredOrCreated(roleName);
			}
		}

		public void RolesInRolesAltered(object sender, EventArgs e)
		{
			//IEnumerable<Role> memberRoles = Event.ExtractParameter<IEnumerable<Role>>(e, 0);
			IEnumerable<Role> targetRoles = Event.ExtractParameter<IEnumerable<Role>>(e, 1);
			foreach (var role in targetRoles)
			{
				foreach (var configuration in _configurations)
				{
					configuration.RoleAlteredOrCreated(role.Name);
				}
			}
		}
	}
}
