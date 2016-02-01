using System.Collections.Generic;
using Sitecore.Security.Accounts;

namespace Unicorn.Roles.Data
{
	public interface IRoleDataStore
	{
		/// <summary>
		/// Gets all roles in the data store
		/// </summary>
		IEnumerable<SyncRoleFile> GetAll();

		/// <summary>
		/// Saves a role into the data store
		/// </summary>
		void Save(Role role);

		/// <summary>
		/// Removes a role from the data store, if it exists.
		/// If it does not exist, does nothing.
		/// </summary>
		void Remove(Role role);

		/// <summary>
		/// Removes all roles from the data store.
		/// </summary>
		void Clear();
	}
}
