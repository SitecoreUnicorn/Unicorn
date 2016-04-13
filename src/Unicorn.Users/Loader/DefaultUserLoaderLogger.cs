using System.Collections.Generic;
using Sitecore.Security.Accounts;
using Unicorn.Logging;
using Unicorn.Users.Data;

namespace Unicorn.Users.Loader
{
	/// <summary>
	/// Handles logging for the role loader
	/// </summary>
	public class DefaultUserLoaderLogger : IUserLoaderLogger
	{
		private readonly ILogger _baseLogger;

		public DefaultUserLoaderLogger(ILogger baseLogger)
		{
			_baseLogger = baseLogger;
		}

		public virtual void MissingRoles(SyncUserFile user, string[] missingRoleNames)
		{
			_baseLogger.Error($"Error deserializing {user.User.UserName} user; some roles it is member of don't exist:{string.Join(", ", missingRoleNames)}.");
		}

		public virtual void AddedNewUser(SyncUserFile user)
		{
			_baseLogger.Info($"[A] User {user.User.UserName}");
		}

		public virtual void UpdatedUser(SyncUserFile user, IEnumerable<UserUpdate> changes)
		{
			_baseLogger.Info($"[U] User {user.User.UserName}");
			foreach (var updatedProperty in changes)
			{
				var role = updatedProperty as UserRoleUpdate;
				if (role == null)
					_baseLogger.Debug($"* [U] {updatedProperty.Property} updated from {updatedProperty.OriginalValue} to {updatedProperty.NewValue}.");
				else
				{
					if(role.Removed) _baseLogger.Debug($"* [D] {role.RoleName} role was removed.");
					else _baseLogger.Debug($"* [A] {role.RoleName} role was added.");
				}
			}
		}

		public virtual void RemovedUser(User user)
		{
			_baseLogger.Warn($"[D] Deleted user {user.Name} because it was not in the serialization store.");
		}
	}
}
