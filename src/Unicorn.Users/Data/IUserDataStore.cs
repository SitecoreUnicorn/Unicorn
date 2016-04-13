using System.Collections.Generic;
using Sitecore.Security.Serialization.ObjectModel;

namespace Unicorn.Users.Data
{
	public interface IUserDataStore
	{
		IEnumerable<SyncUserFile> GetAll();

		void Save(SyncUser user);

		/// <summary>
		/// Removes a user from the data store, if it exists.
		/// If it does not exist, does nothing.
		/// </summary>
		void Remove(string userName);

		/// <summary>
		/// Removes all users from the data store.
		/// </summary>
		void Clear();
	}
}
