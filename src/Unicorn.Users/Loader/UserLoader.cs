namespace Unicorn.Users.Loader
{
  using System.Linq;
  using Sitecore.Diagnostics;
  using Sitecore.Security.Accounts;
  using Sitecore.Security.Serialization;
  using Unicorn.Configuration;
  using Unicorn.Users.Data;
  using Unicorn.Users.Predicates;
  public class UserLoader : IUserLoader
	{
		private readonly IUserPredicate _userPredicate;
		private readonly IUserDataStore _userDataStore;

		public UserLoader(IUserPredicate userPredicate, IUserDataStore userDataStore)
		{
			Assert.ArgumentNotNull(userPredicate, nameof(userPredicate));
			Assert.ArgumentNotNull(userDataStore, nameof(userDataStore));

		  this._userPredicate = userPredicate;
		  this._userDataStore = userDataStore;
		}

		public void Load(IConfiguration configuration)
		{
			using (new UnicornOperationContext())
			{
				var users = this._userDataStore
					.GetAll()
					.Where(user => this._userPredicate.Includes(User.FromName(user.User.UserName, false)).IsIncluded);

				foreach (var user in users)
				{
				  this.DeserializeUser(user);
        }
			}
		}

		protected virtual void DeserializeUser(SyncUserFile user)
		{
		  UserSynchronization.PasteSyncUser(user.User);
		}
	}
}
