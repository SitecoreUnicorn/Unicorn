namespace Unicorn.Data.DataProvider
{
	public class DefaultUnicornDataProviderConfiguration : IUnicornDataProviderConfiguration
	{
		public DefaultUnicornDataProviderConfiguration(bool enableTransparentSync)
		{
			EnableTransparentSync = enableTransparentSync;
		}

		public bool EnableTransparentSync { get; private set; }
	}
}
