using System;
using System.IO;
using System.Web.Hosting;

namespace Unicorn.Configuration
{
	internal static class ConfigurationUtility
	{
		/// <summary>
		/// Applies the available formats for a Unicorn configuration path (i.e. root-relative, absolute) to a given path
		/// </summary>
		public static string ResolveConfigurationPath(string configPath)
		{
			if (configPath.StartsWith("~/"))
			{
				// +1 to Stack Overflow:
				// http://stackoverflow.com/questions/4742257/how-to-use-server-mappath-when-httpcontext-current-is-nothing

				// Support unit testing scenario where hosting environment is not initialized.
				var hostingRoot = HostingEnvironment.IsHosted
					? HostingEnvironment.MapPath("~/")
					: AppDomain.CurrentDomain.BaseDirectory;

				return Path.Combine(hostingRoot, configPath.Substring(2).Replace('/', '\\'));
			}

			return configPath;
		}
	}
}
