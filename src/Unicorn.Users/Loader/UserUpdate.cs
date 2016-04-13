namespace Unicorn.Users.Loader
{
	public class UserUpdate
	{
		public UserUpdate(string property, string originalValue, string newValue, bool deleted = false)
		{
			Property = property;
			OriginalValue = originalValue;
			NewValue = newValue;
			Deleted = deleted;
		}

		public string Property { get; }
		public string OriginalValue { get; }
		public string NewValue { get; }
		public bool Deleted { get; }
	}
}
