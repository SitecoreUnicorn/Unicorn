namespace Unicorn.Users.Loader
{
	public class DefaultUserSyncConfiguration : IUserSyncConfiguration
	{
		private const int DEFAULT_PASSWORD_LENGTH = 8;
		public DefaultUserSyncConfiguration(bool removeOrphans, string defaultPassword, int minPasswordLength)
		{
			RemoveOrphans = removeOrphans;
			DefaultPassword = defaultPassword;
			MinPasswordLength = minPasswordLength <= 0 ? DEFAULT_PASSWORD_LENGTH : minPasswordLength;
		}

		public bool RemoveOrphans { get; }
		public string DefaultPassword { get; }
		public int MinPasswordLength { get; }
	}
}
