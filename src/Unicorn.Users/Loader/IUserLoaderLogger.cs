using System.Collections.Generic;
using Sitecore.Security.Accounts;
using Unicorn.Users.Data;

namespace Unicorn.Users.Loader
{
	public interface IUserLoaderLogger
	{
		void MissingRoles(SyncUserFile user, string[] missingRoleNames);

		void AddedNewUser(SyncUserFile user);

		void UpdatedUser(SyncUserFile user, IEnumerable<UserUpdate> changes);

		void RemovedUser(User user);
	}
}
