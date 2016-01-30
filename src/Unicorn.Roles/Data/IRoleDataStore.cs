using System.Collections.Generic;
using Sitecore.Security.Accounts;

namespace Unicorn.Roles.Data
{
	public interface IRoleDataStore
	{
		IEnumerable<SyncRoleFile> GetAll();
		void Save(Role role);
		void Remove(Role role);
		void Clear();
	}
}
