using System;
using System.Linq;
using Rainbow.Model;

namespace Unicorn.Data
{
	public static class SerializableItemExtensions
	{
		public static string GetDisplayIdentifier(this IItemData item)
		{
			return string.Format("{0}:{1} ({2})", item.DatabaseName, item.Path, item.Id);
		}

		public static bool IsStandardValuesItem(this IItemData item)
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
