using Unicorn.Configuration;

namespace Unicorn.Data.Dilithium
{
	public static class DilithiumExtensions
	{
		public static bool EnablesDilithium(this IConfiguration configuration)
		{
			return configuration.EnablesDilithiumSql() || configuration.EnablesDilithiumSfs();
		}

		public static bool EnablesDilithiumSql(this IConfiguration configuration)
		{
			var diSql = ((ConfigurationDataStore)configuration.Resolve<ISourceDataStore>())?.InnerDataStore as DilithiumSitecoreDataStore;

			return diSql != null;
		}

		public static bool EnablesDilithiumSfs(this IConfiguration configuration)
		{
			var diSfs = ((ConfigurationDataStore)configuration.Resolve<ITargetDataStore>())?.InnerDataStore as DilithiumSerializationFileSystemDataStore;

			return diSfs != null;
		}
	}
}
