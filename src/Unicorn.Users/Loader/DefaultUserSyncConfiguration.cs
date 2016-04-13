namespace Unicorn.Users.Loader
{
	public class DefaultUserSyncConfiguration : IUserSyncConfiguration
	{
		public DefaultUserSyncConfiguration(bool removeOrphans)
		{
			RemoveOrphans = removeOrphans;
		}

		public bool RemoveOrphans { get; }
	}
}
