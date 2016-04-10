using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Security.Accounts;
using Unicorn.Configuration;

namespace Unicorn.Roles.Events
{
	using Sitecore.Data.Events;

	/// <summary>
	/// Sitecore event handler class that hooks to the role system to capture updates for Unicorn
	/// </summary>
	/// <remarks>
	/// Unlike for items, we're using event handlers because it's the best we've got.
	/// This has some implications, in that updates to roles made by actions that disable event handlers,
	/// such as package installations and Sitecore deserialization may be missed by the event handlers.
	/// In that situation you may need to reserialize the configuration after install.
	/// </remarks>
	public class UnicornRolesEventHandler
	{
		private readonly UnicornConfigurationRolesEventHandler[] _configurations;

		public UnicornRolesEventHandler() : this(UnicornConfigurationManager.Configurations)
		{

		}

		protected UnicornRolesEventHandler(IEnumerable<IConfiguration> configurations)
		{
			_configurations = configurations.Select(config => new UnicornConfigurationRolesEventHandler(config)).ToArray();
		}

		public virtual void RoleCreated(object sender, EventArgs e)
		{
			if (EventDisabler.IsActive)
			{
				return;
			}

			string roleName = Event.ExtractParameter<string>(e, 0);

			Assert.IsNotNullOrEmpty(roleName, "Role name was null or empty!");

			foreach (var configuration in _configurations)
			{
				configuration.RoleAlteredOrCreated(roleName);
			}
		}

		public virtual void RoleRemoved(object sender, EventArgs e)
		{
			if (EventDisabler.IsActive)
			{
				return;
			}

			string roleName = Event.ExtractParameter<string>(e, 0);

			Assert.IsNotNullOrEmpty(roleName, "Role name was null or empty!");

			foreach (var configuration in _configurations)
			{
				configuration.RoleDeleted(roleName);
			}
		}

		public virtual void RolesInRolesRemoved(object sender, EventArgs e)
		{
			if (EventDisabler.IsActive)
			{
				return;
			}

			string roleName = Event.ExtractParameter<string>(e, 0);

			Assert.IsNotNullOrEmpty(roleName, "Role name was null or empty!");

			foreach (var configuration in _configurations)
			{
				configuration.RoleAlteredOrCreated(roleName);
			}
		}

		public virtual void RolesInRolesAltered(object sender, EventArgs e)
		{
			IEnumerable<Role> sourceRoles = Event.ExtractParameter<IEnumerable<Role>>(e, 0);
			IEnumerable<Role> targetRoles = Event.ExtractParameter<IEnumerable<Role>>(e, 1);

			if (EventDisabler.IsActive)
			{
				return;
			}

			Assert.IsNotNull(targetRoles, "targetRoles was null!");

			foreach (var role in sourceRoles)
			{
				foreach (var configuration in _configurations)
				{
					configuration.RoleAlteredOrCreated(role.Name);
				}
			}
		}
	}
}
