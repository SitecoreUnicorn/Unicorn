using System;
using System.Linq;
using Sitecore.Data.Managers;
using Sitecore.Globalization;

namespace Unicorn.Serialization
{
	public static class SerializedItemExtensions
	{
		public static SerializedVersion GetVersion(this ISerializedItem serializedItem, string language, int versionNumber)
		{
			if (language.Equals(Language.Invariant.Name)) language = LanguageManager.DefaultLanguage.Name;

			return serializedItem.Versions.FirstOrDefault(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase) && x.VersionNumber == versionNumber);
		
		}
	}
}
