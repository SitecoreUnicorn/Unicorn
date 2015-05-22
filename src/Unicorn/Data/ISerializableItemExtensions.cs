using System;
using System.Linq;
using Gibson.Model;

namespace Unicorn.Data
{
	public static class SerializableItemExtensions
	{
		/// <summary>
		/// Helper method to get a specific version from a source item, if it exists
		/// </summary>
		/// <returns>Null if the version does not exist or the version if it exists</returns>
		public static ISerializableVersion GetVersion(this ISerializableItem sourceItem, string language, int versionNumber)
		{
			return sourceItem.Versions.FirstOrDefault(x => x.Language.Name.Equals(language, StringComparison.OrdinalIgnoreCase) && x.VersionNumber == versionNumber);
		}

		public static string GetDisplayIdentifier(this ISerializableItem item)
		{
			return string.Format("{0}:{1} ({2})", item.DatabaseName, item.Path, item.Id);
		}

		public static bool IsStandardValuesItem(this ISerializableItem item)
		{
			string[] array = item.Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length > 0)
			{
				if (array.Any(s => s.Equals("templates", StringComparison.OrdinalIgnoreCase)))
				{
					return array.Last().Equals("__Standard Values", StringComparison.OrdinalIgnoreCase);
				}
			}

			return false;
		}
	}
}
