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
	}
}