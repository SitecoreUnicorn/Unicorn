using Rainbow.Model;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	public class ChangeEntry
	{
		public ChangeEntry(ISerializableItem item, ChangeType type)
		{
			Item = item;
			ChangeType = type;
		}

		public ISerializableItem Item { get; private set; }
		public ChangeType ChangeType { get; private set; }
	}
}
