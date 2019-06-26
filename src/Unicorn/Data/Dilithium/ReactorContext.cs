using Sitecore.Diagnostics;
using Unicorn.Data.Dilithium.Rainbow;
using Unicorn.Data.Dilithium.Sql;

namespace Unicorn.Data.Dilithium
{
	public static class ReactorContext
	{
		public static SqlPrecacheStore SqlPrecache { get; set; }
		public static RainbowPrecacheStore RainbowPrecache { get; set; }

		public static bool IsActive => SqlPrecache != null || RainbowPrecache != null;

		public static void Dispose()
		{
			if (SqlPrecache != null)
			{
				Log.Info("[Unicorn] Dilithium SQL context has been released.", typeof(ReactorContext));

				SqlPrecache = null;
			}

			if (RainbowPrecache != null)
			{
				Log.Info("[Unicorn] Dilithium Rainbow context has been released.", typeof(ReactorContext));

				RainbowPrecache = null;
			}
		}
	}
}
