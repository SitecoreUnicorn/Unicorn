using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Unicorn.Configuration;
using Unicorn.Users.Data;
using Unicorn.Users.Predicates;

namespace Unicorn.Users.Events
{
	public class UnicornConfigurationUsersEventHandler
	{
		private readonly IUserPredicate _predicate;
		private readonly IUserDataStore _dataStore;

		public UnicornConfigurationUsersEventHandler(IConfiguration configuration)
		{
			Assert.ArgumentNotNull(configuration, nameof(configuration));

			_predicate = configuration.Resolve<IUserPredicate>();
			_dataStore = configuration.Resolve<IUserDataStore>();
		}


		public virtual void UserAlteredOrCreated(string userName)
		{
			var user = User.FromName(userName, false);
			if (_predicate == null || !_predicate.Includes(user).IsIncluded)
			{
				return;
			}

			_dataStore.Save(user);
		}

		public void UserDeleted(string userName)
		{
			var user = User.FromName(userName, false);

			if (_predicate == null || !_predicate.Includes(user).IsIncluded) return;

			_dataStore.Remove(user);
		}
	}
}
