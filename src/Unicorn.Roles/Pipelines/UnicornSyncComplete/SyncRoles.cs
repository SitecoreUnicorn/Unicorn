using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncComplete;
using Unicorn.Roles.Loader;
using Unicorn.Roles.RolePredicates;

namespace Unicorn.Roles.Pipelines.UnicornSyncComplete
{
	public class SyncRoles : IUnicornSyncCompleteProcessor
	{
		public void Process(UnicornSyncCompletePipelineArgs args)
		{
			var rolePredicate = args.Configuration.Resolve<IRolePredicate>();

			// no predicate = configuration doesn't include any roles
			if (rolePredicate == null) return;

			var loader = args.Configuration.Resolve<IRoleLoader>();
			var logger = args.Configuration.Resolve<ILogger>();
			
			logger.Info(string.Empty);
			logger.Info($"{args.Configuration.Name} roles are being synced.");

			loader.Load(args.Configuration);

			logger.Info($"{args.Configuration.Name} role sync complete.");
		}
	}
}
