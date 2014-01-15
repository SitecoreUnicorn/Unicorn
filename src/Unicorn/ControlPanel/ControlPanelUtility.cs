using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.ControlPanel
{
	public static class ControlPanelUtility
	{
		/// <summary>
		/// Checks if any of the current predicate's root paths exist in the serialization provider
		/// </summary>
		public static bool HasAnySerializedItems(IPredicate predicate, ISerializationProvider serializationProvider)
		{
			var rootItems = predicate.GetRootItems();
			bool hasSerializedItems = false;

			foreach (var rootItem in rootItems)
			{
				var reference = serializationProvider.GetReference(rootItem);
				if (reference == null || reference.GetItem() == null)
				{
					continue;
				}

				hasSerializedItems = true;
				break;
			}

			return hasSerializedItems;
		}
	}
}
