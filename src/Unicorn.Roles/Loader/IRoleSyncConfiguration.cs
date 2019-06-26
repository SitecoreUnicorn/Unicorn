namespace Unicorn.Roles.Loader
{
	/// <summary>
	/// Controls how a role sync configuration syncs.
	/// </summary>
	public interface IRoleSyncConfiguration
	{
		/// <summary>
		/// If true orphaned roles that are not serialized, but match the configuration's predicate are deleted from Sitecore
		/// This is similar to SerializedAsMasterEvaluator for Unicorn.
		/// </summary>
		bool RemoveOrphans { get; }
	}
}