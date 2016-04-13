namespace Unicorn.Users.Loader
{
	public class UserUpdate
	{
		public UserUpdate(string property, string originalValue, string newValue)
		{
			Property = property;
			OriginalValue = originalValue;
			NewValue = newValue;
		}

		public string Property { get; }
		public string OriginalValue { get; }
		public string NewValue { get; }
	}
}
