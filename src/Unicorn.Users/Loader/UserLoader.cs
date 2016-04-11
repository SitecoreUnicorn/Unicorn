using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Sitecore.Security.Serialization;
using Unicorn.Configuration;
using Unicorn.Users.Data;
using Unicorn.Users.Predicates;

namespace Unicorn.Users.Loader
{
	public class UserLoader : IUserLoader
	{
		private readonly IUserPredicate _userPredicate;
		private readonly IUserDataStore _userDataStore;

		public UserLoader(IUserPredicate userPredicate, IUserDataStore userDataStore)
		{
			Assert.ArgumentNotNull(userPredicate, nameof(userPredicate));
			Assert.ArgumentNotNull(userDataStore, nameof(userDataStore));

			_userPredicate = userPredicate;
			_userDataStore = userDataStore;
		}

		public void Load(IConfiguration configuration)
		{
			using (new UnicornOperationContext())
			{
				var users = _userDataStore
					.GetAll()
					.Where(user => _userPredicate.Includes(User.FromName(user.User.UserName, false)).IsIncluded);

				foreach (var user in users)
				{
					DeserializeUser(user);
				}
			}
		}

		protected virtual void DeserializeUser(SyncUserFile user)
		{
			UserSynchronization.PasteSyncUser(user.User);
		}
	}
}
