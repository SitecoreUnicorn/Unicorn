using System;
using System.Linq;
using Sitecore.Data.Managers;
using Sitecore.Globalization;

namespace Unicorn.Data
{
	public static class SourceItemExtensions
	{
		public static ItemVersion GetVersion(this ISourceItem sourceItem, string language, int versionNumber)
		{
			if (language.Equals(Language.Invariant.Name)) language = LanguageManager.DefaultLanguage.Name;

			return sourceItem.Versions.FirstOrDefault(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase) && x.VersionNumber == versionNumber);
		
		}
	}
}
