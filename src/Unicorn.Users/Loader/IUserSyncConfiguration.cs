namespace Unicorn.Users.Loader
{
	/// <summary>
	/// Controls how a user sync configuration syncs.
	/// </summary>
	public interface IUserSyncConfiguration
	{
		/// <summary>
		/// If true orphaned users that are not serialized, but match the configuration's predicate are deleted from Sitecore
		/// This is similar to SerializedAsMasterEvaluator for Unicorn.
		/// </summary>
		bool RemoveOrphans { get; }

		/// <summary>
		/// When NEW users are deserialized, their passwords will be set to this value. 
		/// If the value is set to "random," the password will be set to a long randomly generated value, otherwise the literal value is used.
		/// </summary>
		string DefaultPassword { get; }
		
		/// <summary>
		/// If defaultPassword is not random, this settings defines the minimum accepted password length when deserializing a user.
		/// Default is 8 and must be larger than 0.
		/// </summary>
		int MinPasswordLength { get; }
	}
}