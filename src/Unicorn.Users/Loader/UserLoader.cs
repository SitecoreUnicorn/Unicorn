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
			// TODO: cannot currently detect changes here, so nothing will get logged
			ProfileManager.DeleteProfile(updatedUser.UserName);

			foreach (string name in SiteContextFactory.GetSiteNames())
			{
				SiteContext siteContext = SiteContextFactory.GetSiteContext(name);
				siteContext?.Caches.RegistryCache.RemoveKeysContaining(updatedUser.UserName);
			}

			User user = User.FromName(serializedUser.UserName, true);

			foreach (string propertyName in user.Profile.GetCustomPropertyNames())
				user.Profile.RemoveCustomProperty(propertyName);

			foreach (SyncProfileProperty syncProfileProperty in serializedUser.ProfileProperties)
			{
				if (syncProfileProperty.IsCustomProperty)
					user.Profile.SetCustomProperty(syncProfileProperty.Name, (string)syncProfileProperty.Content);
				else
					user.Profile.SetPropertyValue(syncProfileProperty.Name, syncProfileProperty.Content);
			}

			user.Profile.Save();
			CacheManager.GetUserProfileCache().RemoveUser(updatedUser.UserName);
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
