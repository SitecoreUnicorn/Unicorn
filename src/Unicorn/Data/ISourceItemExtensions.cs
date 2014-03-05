using System;
using System.Linq;

namespace Unicorn.Data
{
	public static class SourceItemExtensions
	{
		/// <summary>
		/// Helper method to get a specific version from a source item, if it exists
		/// </summary>
		/// <returns>Null if the version does not exist or the version if it exists</returns>
		public static ItemVersion GetVersion(this ISourceItem sourceItem, string language, int versionNumber)
		{
			return sourceItem.Versions.FirstOrDefault(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase) && x.VersionNumber == versionNumber);
		}
	}
}
