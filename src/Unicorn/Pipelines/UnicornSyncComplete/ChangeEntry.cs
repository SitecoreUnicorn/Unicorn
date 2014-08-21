using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	public class ChangeEntry
	{
		public ChangeEntry(ISerializedItem item, ChangeType type)
		{
			SerializedItem = item;
			ChangeType = type;
		}

		public ChangeEntry(ISourceItem item, ChangeType type)
		{
			SourceItem = item;
			ChangeType = type;
		}

		public ISerializedItem SerializedItem { get; private set; }
		public ISourceItem SourceItem { get; private set; }
		public ChangeType ChangeType { get; private set; }
	}
}
