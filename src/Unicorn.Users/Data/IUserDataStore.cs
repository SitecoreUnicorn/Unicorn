using System.Collections.Generic;
using Sitecore.Security.Accounts;

namespace Unicorn.Users.Data
{
	public interface IUserDataStore
	{
		IEnumerable<SyncUserFile> GetAll();

		void Save(User user);

		/// <summary>
		/// Removes a user from the data store, if it exists.
		/// If it does not exist, does nothing.
		/// </summary>
		void Remove(User user);

		/// <summary>
		/// Removes all users from the data store.
		/// </summary>
		void Clear();
	}
}
