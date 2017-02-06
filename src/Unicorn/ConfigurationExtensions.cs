using System.Collections.Generic;
using System.Linq;
using Unicorn.Configuration;
using Unicorn.Data.DataProvider;

namespace Unicorn
{
	public static class ConfigurationExtensions
	{
		public static IEnumerable<IConfiguration> SkipTransparentSync(this IEnumerable<IConfiguration> configurations)
		{
			return configurations.Where(configuration => !configuration.Resolve<IUnicornDataProviderConfiguration>().EnableTransparentSync);
		}
	}
}
