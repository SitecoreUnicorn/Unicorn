using System.Collections.Generic;
using Unicorn.Roles.Model;

namespace Unicorn.Roles.Data
{
	public interface IRoleDataStore
	{
		/// <summary>
		/// Gets all roles in the data store
		/// </summary>
		IEnumerable<IRoleData> GetAll();

		/// <summary>
		/// Saves a role into the data store
		/// </summary>
		void Save(IRoleData role);

		/// <summary>
		/// Removes a role from the data store, if it exists.
		/// If it does not exist, does nothing.
		/// </summary>
		void Remove(IRoleData role);

		/// <summary>
		/// Removes all roles from the data store.
		/// </summary>
		void Clear();
	}
}
