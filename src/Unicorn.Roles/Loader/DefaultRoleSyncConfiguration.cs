namespace Unicorn.Roles.Loader
{
	public class DefaultRoleSyncConfiguration : IRoleSyncConfiguration
	{
		public DefaultRoleSyncConfiguration(bool removeOrphans)
		{
			RemoveOrphans = removeOrphans;
		}

		public bool RemoveOrphans { get; }
	}
}
