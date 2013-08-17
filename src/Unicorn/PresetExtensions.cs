using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;

namespace Unicorn
{
	/// <summary>
	/// Extension methods on Serialization Preset API classes to make checks easier
	/// </summary>
	internal static class PresetExtensions
	{
		/// <summary>
		/// Checks if a list of presets includes a given item
		/// </summary>
		public static bool Includes(this IList<IncludeEntry> entries, Item item)
		{
			// no entries = include everything
			if (entries.FirstOrDefault() == null) return true;

			return entries.Any(entry => entry.Includes(item));
		}

		/// <summary>
		/// Checks if a preset includes a given item
		/// </summary>
		public static bool Includes(this IncludeEntry entry, Item item)
		{
			// check for db match
			if (item.Database.Name != entry.Database) return false;

			// check for path match
			if (!item.Paths.Path.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase)) return false;

			// check excluded paths (*our* path exclusions are case-insensitive, whereas Sitecore's are case-sensitive - this can result in unexpected deletions)
			if (entry.Exclude.Any(x => x.Type == "path" && item.Paths.FullPath.StartsWith(x.Value, StringComparison.OrdinalIgnoreCase))) return false;

			// check excludes
			if (entry.Exclude.Any(exclude => exclude.Matches(item))) return false;

			// we passed all the checks - we're included by the current rule
			return true;
		}

		/// <summary>
		/// Checks if a given list of presets matches a specific Sitecore path
		/// </summary>
		public static bool MatchesPath(this IEnumerable<ExcludeEntry> entries, string path)
		{
			var itemPath = "/" + string.Join("/", PathUtils.MakeItemPath(path).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1));

			return entries.Any(entry => entry.Type == "path" && entry.Value.Equals(itemPath, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Checks if a given list of presets matches a specific item ID. Use ID.ToString() format eg {A9F4...}
		/// </summary>
		public static bool MatchesId(this IEnumerable<ExcludeEntry> entries, string id)
		{
			return entries.Any(entry => entry.Type == "id" && entry.Value.Equals(id, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Checks if a given list of presets matches a specific template name
		/// </summary>
		public static bool MatchesTemplate(this IEnumerable<ExcludeEntry> entries, string templateName)
		{
			return entries.Any(entry => entry.Type == "template" && entry.Value.Equals(templateName, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Checks if a given list of presets matches a specific template ID
		/// </summary>
		public static bool MatchesTemplateId(this IEnumerable<ExcludeEntry> entries, string templateId)
		{
			return entries.Any(entry => entry.Type == "templateid" && entry.Value.Equals(templateId, StringComparison.OrdinalIgnoreCase));
		}
	}
}
