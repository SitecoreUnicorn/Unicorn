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
	}
}