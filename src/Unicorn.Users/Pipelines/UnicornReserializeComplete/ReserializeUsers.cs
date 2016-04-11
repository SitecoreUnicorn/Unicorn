using System.Linq;
using Sitecore.Security.Accounts;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornReserializeComplete;
using Unicorn.Users.Data;
using Unicorn.Users.Predicates;


namespace Unicorn.Users.Pipelines.UnicornReserializeComplete
{
	public class ReserializeUsers : IUnicornReserializeCompleteProcessor
	{
		public virtual void Process(UnicornReserializeCompletePipelineArgs args)
		{

			var userPredicate = args.Configuration.Resolve<IUserPredicate>();

			// no predicate = configuration doesn't include any users
			if (userPredicate == null) return;

			var dataStore = args.Configuration.Resolve<IUserDataStore>();
			var logger = args.Configuration.Resolve<ILogger>();

			logger.Info(string.Empty);
			logger.Info($"{args.Configuration.Name} users are being reserialized.");

			dataStore.Clear();

			var users = UserManager.GetUsers().Where(user => userPredicate.Includes(user).IsIncluded);

			int userCount = 0;

			foreach (var user in users)
			{
				dataStore.Save(user);
				userCount++;
			}

			logger.Info($"{args.Configuration.Name} users reserialize complete. {userCount} users were reserialized.");
			logger.Info(string.Empty);
		}
	}
}
