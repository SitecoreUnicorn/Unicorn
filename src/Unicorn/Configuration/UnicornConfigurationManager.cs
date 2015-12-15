using Sitecore.Configuration;
using Unicorn.ControlPanel.Security;

namespace Unicorn.Configuration
{
	/// <summary>
	/// This is the primary class to read configurations with. It reads the configuration provider from Unicorn.config and loads its configurations per its implementation.
	/// </summary>
	public static class UnicornConfigurationManager
	{
		private static readonly IConfigurationProvider Instance;

		static UnicornConfigurationManager()
		{
			Instance = (IConfigurationProvider) Factory.CreateObject("/sitecore/unicorn/configurationProvider", true);
			AuthenticationProvider = (IUnicornAuthenticationProvider)Factory.CreateObject("/sitecore/unicorn/authenticationProvider", false);
		}

		public static IConfiguration[] Configurations => Instance.Configurations;
	
		public static IUnicornAuthenticationProvider AuthenticationProvider { get; }
	}
}
