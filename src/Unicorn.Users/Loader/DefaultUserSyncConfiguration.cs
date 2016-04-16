namespace Unicorn.Users.Loader
{
	public class DefaultUserSyncConfiguration : IUserSyncConfiguration
	{
		public DefaultUserSyncConfiguration(bool removeOrphans, string defaultPassword)
		{
			RemoveOrphans = removeOrphans;
			DefaultPassword = defaultPassword;
		}

		public bool RemoveOrphans { get; }
		public string DefaultPassword { get; }
	}
}
