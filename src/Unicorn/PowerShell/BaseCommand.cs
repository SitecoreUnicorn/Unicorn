using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Unicorn.PowerShell
{
	// Ported code from the Sitecore PowerShell Extensions module.
	public class BaseCommand : PSCmdlet
	{
		protected static IEnumerable<T> WildcardFilter<T>(string filter, IEnumerable<T> items,
			Func<T, string> propertyName)
		{
			return WildcardUtils.WildcardFilter(filter, items, propertyName);
		}
	}

	internal static class WildcardUtils
	{
		public static WildcardPattern GetWildcardPattern(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				name = "*";
			}
			const WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
			var wildcard = new WildcardPattern(name, options);
			return wildcard;
		}

		public static IEnumerable<T> WildcardFilter<T>(string filter, IEnumerable<T> items,
			Func<T, string> propertyName)
		{
			var wildcardPattern = GetWildcardPattern(filter);
			return items.Where(item => wildcardPattern.IsMatch(propertyName(item)));
		}
	}
}
