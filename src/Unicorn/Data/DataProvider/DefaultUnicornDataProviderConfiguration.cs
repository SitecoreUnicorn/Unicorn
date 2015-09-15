namespace Unicorn.Data.DataProvider
{
	/// <summary>
	/// Provides configuration settings to the Unicorn Data Provider that are configuration specific
	/// </summary>
	public class DefaultUnicornDataProviderConfiguration : IUnicornDataProviderConfiguration
	{
		public DefaultUnicornDataProviderConfiguration(bool enableTransparentSync)
		{
			EnableTransparentSync = enableTransparentSync;
		}

		/// <summary>
		/// Enables Transparent Sync (data provider is allowed to read from serialization store as well as write)
		/// </summary>
		public bool EnableTransparentSync { get; private set; }
	}
}
