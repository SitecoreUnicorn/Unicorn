using Sitecore.Diagnostics;

namespace Unicorn.Data.Dilithium
{
	public static class ReactorContext
	{
		public static DilithiumReactor Reactor { get; set; }

		public static void Dispose()
		{
			if (Reactor != null)
			{
				Log.Info("[Unicorn] Dilithium context has been released.", typeof(ReactorContext));

				Reactor = null;
			}
		}
	}
}
