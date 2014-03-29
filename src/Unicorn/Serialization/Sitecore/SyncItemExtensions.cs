using System;
using System.Globalization;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Globalization;

namespace Unicorn.Serialization.Sitecore
{
	internal static class SyncItemExtensions
	{
		public static SyncVersion GetVersion(this SyncItem item, VersionUri uri)
		{
			string versionString = uri.Version.Number.ToString(CultureInfo.InvariantCulture);

			var language = uri.Language;

			if (language.Equals(Language.Invariant)) language = LanguageManager.DefaultLanguage;

			return item.Versions.FirstOrDefault(x => x.Language.Equals(language.Name, StringComparison.OrdinalIgnoreCase) && x.Version == versionString);
		}
	}
}
