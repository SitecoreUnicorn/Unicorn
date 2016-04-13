using System.Linq;
using System.Web.Security;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornReserializeComplete;
using Unicorn.Users.Data;
using Unicorn.Users.UserPredicates;


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

			Assert.IsNotNull(dataStore, $"The IUserDataStore was not registered for the {args.Configuration.Name} configuration.");
			Assert.IsNotNull(logger, $"The ILogger was not registered for the {args.Configuration.Name} configuration.");

			logger.Info(string.Empty);
			logger.Info($"{args.Configuration.Name} users are being reserialized.");

			dataStore.Clear();

			var users = UserManager.GetUsers().Where(user => userPredicate.Includes(user).IsIncluded);

			int userCount = 0;

			foreach (var user in users)
			{
				dataStore.Save(UserSynchronization.BuildSyncUser(Membership.GetUser(user.Name)));
				userCount++;
			}

			logger.Info($"{args.Configuration.Name} users reserialize complete. {userCount} users were reserialized.");
			logger.Info(string.Empty);
		}
	}
}
