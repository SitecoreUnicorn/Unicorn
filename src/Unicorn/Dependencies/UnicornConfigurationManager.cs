using Sitecore.Configuration;

namespace Unicorn.Dependencies
{
	public static class UnicornConfigurationManager
	{
		private static readonly IConfigurationProvider Instance;
		static UnicornConfigurationManager()
		{
			Instance = (IConfigurationProvider) Factory.CreateObject("/sitecore/unicorn/configurationProvider", true);
		}

		public static IConfiguration[] Configurations { get { return Instance.Configurations; } }
	}
}
