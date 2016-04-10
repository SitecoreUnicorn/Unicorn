using System.Linq;
using Sitecore.Security.Accounts;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornReserializeComplete;
using Unicorn.Roles.Data;
using Unicorn.Roles.Model;
using Unicorn.Roles.RolePredicates;

namespace Unicorn.Roles.Pipelines.UnicornReserializeComplete
{
	public class ReserializeRoles : IUnicornReserializeCompleteProcessor
	{
		public void Process(UnicornReserializeCompletePipelineArgs args)
		{

			var rolePredicate = args.Configuration.Resolve<IRolePredicate>();

			// no predicate = configuration doesn't include any roles
			if (rolePredicate == null) return;

			var dataStore = args.Configuration.Resolve<IRoleDataStore>();
			var logger = args.Configuration.Resolve<ILogger>();

			logger.Info(string.Empty);
			logger.Info($"{args.Configuration.Name} roles are being reserialized.");

			dataStore.Clear();

			var roles = RolesInRolesManager.GetAllRoles()
				.Select(role => new SitecoreRoleData(role))
				.Where(role => rolePredicate.Includes(role).IsIncluded);

			int roleCount = 0;

			foreach (var role in roles)
			{
				dataStore.Save(role);
				roleCount++;
			}

			logger.Info($"{args.Configuration.Name} role reserialize complete. {roleCount} roles were reserialized.");
			logger.Info(string.Empty);
		}
	}
}
