namespace Unicorn.Loader
{
	/// <summary>
	/// Controls how a configuration syncs.
	/// </summary>
	public interface ISyncConfiguration
	{
		/// <summary>
		/// If true changed items are updated in indexes
		/// </summary>
		bool UpdateLinkDatabase { get; }

		/// <summary>
		/// If true changed items are updated in link database
		/// </summary>
		bool UpdateSearchIndex { get; }

		/// <summary>
		/// How many threads to sync with. Use 1 if: syncing templates or Sitecore older than Sitecore 8 Update 3. Use 16 or so otherwise.
		/// </summary>
		int MaxConcurrency { get; }
	}
}