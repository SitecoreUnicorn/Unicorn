using Rainbow.Model;

namespace Unicorn.Data
{
	public static class SerializableItemExtensions
	{
		public static string GetDisplayIdentifier(this IItemData item)
		{
			return string.Format("{0}:{1} ({2})", item.DatabaseName, item.Path, item.Id);
		}
	}
}
