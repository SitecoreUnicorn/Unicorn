using Rainbow.Model;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	public class ChangeEntry
	{
		public ChangeEntry(IItemData itemData, ChangeType type)
		{
			ItemData = itemData;
			ChangeType = type;
		}

		public IItemData ItemData { get; private set; }
		public ChangeType ChangeType { get; private set; }
	}
}
