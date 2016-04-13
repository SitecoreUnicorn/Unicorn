using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Profile;
using System.Web.Security;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization.ObjectModel;
using Sitecore.Sites;
using Unicorn.Configuration;
using Unicorn.Users.Data;
using Unicorn.Users.UserPredicates;

namespace Unicorn.Users.Loader
{
	public class UserLoader : IUserLoader
	{
		private readonly IUserPredicate _userPredicate;
		private readonly IUserDataStore _userDataStore;
		private readonly IUserLoaderLogger _logger;

		public UserLoader(IUserPredicate userPredicate, IUserDataStore userDataStore, IUserLoaderLogger logger)
		{
			Assert.ArgumentNotNull(userPredicate, nameof(userPredicate));
			Assert.ArgumentNotNull(userDataStore, nameof(userDataStore));
			Assert.ArgumentNotNull(logger, nameof(logger));

			_userPredicate = userPredicate;
			_userDataStore = userDataStore;
			_logger = logger;
		}

		public void Load(IConfiguration configuration)
		{
			using (new UnicornOperationContext())
			{
				var users = _userDataStore
					.GetAll()
					.Where(user => _userPredicate.Includes(User.FromName(user.User.UserName, false)).IsIncluded).ToArray();

				foreach (var user in users)
				{
					DeserializeUser(user);
				}

				var existingOrphanUsers = UserManager.GetUsers()
					.GetAll()
					.Where(user => _userPredicate.Includes(user).IsIncluded)
					.Where(includedUser => !users.Any(user => user.User.UserName.Equals(includedUser.Name, StringComparison.OrdinalIgnoreCase)));

				foreach (var orphan in existingOrphanUsers)
				{
					_logger.RemovedUser(orphan);
					Membership.DeleteUser(orphan.Name);
				}
			}
		}

		protected virtual void DeserializeUser(SyncUserFile serializedUser)
		{
			var changes = new List<UserUpdate>();

			var syncUser = serializedUser.User;

			List<string> missingRolesForUser = syncUser.Roles.FindAll(roleName => !Roles.RoleExists(roleName));

			if (missingRolesForUser.Count > 0)
			{
				_logger.MissingRoles(serializedUser, missingRolesForUser.ToArray());
				return;
			}

			bool addedUser;
			var existingUser = GetOrCreateUser(serializedUser, out addedUser);

			MembershipUser updatedUser = new MembershipUser(
				existingUser.ProviderName,
				syncUser.UserName,
				existingUser.ProviderUserKey,
				syncUser.Email,
				null,
				syncUser.Comment,
				syncUser.IsApproved,
				existingUser.IsLockedOut,
				syncUser.CreationDate,
				existingUser.LastLoginDate,
				existingUser.LastActivityDate,
				addedUser ? DateTime.UtcNow : existingUser.LastPasswordChangedDate,
				existingUser.LastLockoutDate);

			if (!addedUser)
			{
				if (!existingUser.Email.Equals(updatedUser.Email)) changes.Add(new UserUpdate("Email", existingUser.Email, updatedUser.Email));
				if (!existingUser.Comment.Equals(updatedUser.Comment)) changes.Add(new UserUpdate("Comment", existingUser.Comment, updatedUser.Comment));
				if (!existingUser.IsApproved.Equals(updatedUser.IsApproved)) changes.Add(new UserUpdate("IsApproved", existingUser.IsApproved.ToString(), updatedUser.IsApproved.ToString()));
				if (!existingUser.CreationDate.Equals(updatedUser.CreationDate.ToUniversalTime())) changes.Add(new UserUpdate("CreationDate", existingUser.CreationDate.ToString(CultureInfo.InvariantCulture), updatedUser.CreationDate.ToUniversalTime().ToString(CultureInfo.InvariantCulture) + " (UTC)"));
			}

			Membership.UpdateUser(updatedUser);

			PasteProfileValues(updatedUser, syncUser, addedUser, changes);

			PasteRoles(syncUser, User.FromName(serializedUser.User.UserName, true), addedUser, changes);

			if (changes.Count > 0)
				_logger.UpdatedUser(serializedUser, changes);
		}

		protected virtual MembershipUser GetOrCreateUser(SyncUserFile user, out bool addedUser)
		{
			addedUser = false;

			var existingUser = Membership.GetUser(user.User.UserName);

			if (existingUser != null) return existingUser;

			addedUser = true;
			_logger.AddedNewUser(user);

			return Membership.CreateUser(user.User.UserName, CreateNewUserPassword(user));
		}

		protected virtual string CreateNewUserPassword(SyncUserFile user)
		{
			var password = Settings.GetSetting("Unicorn.Users.DefaultPassword");

			if (password.Equals("random", StringComparison.OrdinalIgnoreCase))
			{
				return Membership.GeneratePassword(32, 0);
			}

			if (password.Length < 8) throw new InvalidOperationException("I will not set the default user password to anything less than 8 characters. Change your Unicorn.Users.DefaultPassword setting to something more secure.");

			return password;
		}

		protected virtual void PasteProfileValues(MembershipUser updatedUser, SyncUser serializedUser, bool addedUser, List<UserUpdate> changes)
		{
			foreach (string name in SiteContextFactory.GetSiteNames())
			{
				SiteContext siteContext = SiteContextFactory.GetSiteContext(name);
				siteContext?.Caches.RegistryCache.RemoveKeysContaining(updatedUser.UserName);
			}

			User user = User.FromName(serializedUser.UserName, true);

			bool propertiesAreUpdated = false;

			// load custom properties
			var knownCustomProperties = new HashSet<string>();
			foreach (var customProperty in serializedUser.ProfileProperties.Where(property => property.IsCustomProperty))
			{
				knownCustomProperties.Add(customProperty.Name);

				// check if we need to change the value
				var existingValue = user.Profile.GetCustomProperty(customProperty.Name);
				if (existingValue != null && existingValue.Equals((string) customProperty.Content, StringComparison.Ordinal)) continue;

				propertiesAreUpdated = true;
				user.Profile.SetCustomProperty(customProperty.Name, (string)customProperty.Content);
				changes.Add(new UserUpdate(customProperty.Name, existingValue, (string)customProperty.Content));
			}

			// cull orphan custom properties
			foreach (var existingCustomProperty in user.Profile.GetCustomPropertyNames())
			{
				if (!knownCustomProperties.Contains(existingCustomProperty))
				{
					propertiesAreUpdated = true;
					changes.Add(new UserUpdate(existingCustomProperty, user.Profile.GetCustomProperty(existingCustomProperty), "Deleted", true));
					user.Profile.RemoveCustomProperty(existingCustomProperty);
				}
			}

			// load standard properties
			var knownStandardProperties = new HashSet<string>();
			foreach (var standardProperty in serializedUser.ProfileProperties.Where(property => !property.IsCustomProperty))
			{
				knownStandardProperties.Add(standardProperty.Name);

				// check if we need to change the value
				var existingValue = user.Profile.GetPropertyValue(standardProperty.Name);

				if (existingValue != null && (existingValue.GetType().IsPrimitive || existingValue is string))
				{
					if (existingValue.Equals(standardProperty.Content)) continue;

					propertiesAreUpdated = true;
					user.Profile.SetPropertyValue(standardProperty.Name, standardProperty.Content);
					changes.Add(new UserUpdate(standardProperty.Name, existingValue.ToString(), standardProperty.Content.ToString()));
				}
				else
				{
					// a custom serialized type. No good way to compare as we don't know if it is at all comparable.
					// we'll go with a quiet always update here
					propertiesAreUpdated = true;
					user.Profile.SetPropertyValue(standardProperty.Name, standardProperty.Content);
					if (existingValue == null) changes.Add(new UserUpdate(standardProperty.Name, "null", standardProperty.Content.ToString()));
				}
			}

			if (propertiesAreUpdated)
			{
				user.Profile.Save();

				CacheManager.GetUserProfileCache().RemoveUser(updatedUser.UserName);
			}
		}

		protected virtual void PasteRoles(SyncUser serializedUser, User sitecoreUser, bool addedUser, List<UserUpdate> changes)
		{
			foreach (var role in serializedUser.Roles)
			{
				if (!sitecoreUser.Roles.Any(ur => ur.Name.Equals(role, StringComparison.OrdinalIgnoreCase)))
				{
					sitecoreUser.Roles.Add(Role.FromName(role));
					if (!addedUser) changes.Add(new UserRoleUpdate(role, false));
				}
			}

			foreach (var orphanRole in sitecoreUser.Roles.Where(role => !serializedUser.Roles.Any(sr => sr.Equals(role.Name, StringComparison.OrdinalIgnoreCase))))
			{
				sitecoreUser.Roles.Remove(orphanRole);
				if (!addedUser) changes.Add(new UserRoleUpdate(orphanRole.Name, true));
			}
		}
	}
}
