namespace Unicorn.Users.Loader
{
	public class UserRoleUpdate : UserUpdate
	{
		public string RoleName { get; }
		public bool Removed { get; }

		public UserRoleUpdate(string roleName, bool removed) : base(null, null, null)
		{
			RoleName = roleName;
			Removed = removed;
		}
	}
}
