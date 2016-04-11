using Unicorn.Pipelines.UnicornSyncComplete;
using Unicorn.Logging;
using Unicorn.Users.Loader;
using Unicorn.Users.Predicates;

namespace Unicorn.Users.Pipelines.UnicornSyncComplete
{
	public class SyncUsers : IUnicornSyncCompleteProcessor
	{
		public void Process(UnicornSyncCompletePipelineArgs args)
		{
			var userPredicate = args.Configuration.Resolve<IUserPredicate>();

			// no predicate = configuration doesn't include any users
			if (userPredicate == null) return;

			var loader = args.Configuration.Resolve<IUserLoader>();
			var logger = args.Configuration.Resolve<ILogger>();

			logger.Info(string.Empty);
			logger.Info($"{args.Configuration.Name} users are being synced.");

			loader.Load(args.Configuration);

			logger.Info($"{args.Configuration.Name} user sync complete.");
		}
	}
}
