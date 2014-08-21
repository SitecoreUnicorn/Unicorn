using System.Collections.Generic;
using System.Linq;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	public class DefaultSyncCompleteDataCollector: ISyncCompleteDataCollector
	{
		private readonly Queue<ChangeEntry> _entries = new Queue<ChangeEntry>();
 
		public void PushChangedItem(ISerializedItem serializedItem, ChangeType type)
		{
			_entries.Enqueue(new ChangeEntry(serializedItem, type));
		}

		public void PushChangedItem(ISourceItem sourceItem, ChangeType type)
		{
			_entries.Enqueue(new ChangeEntry(sourceItem, type));
		}

		public IReadOnlyCollection<ChangeEntry> GetChanges()
		{
			return _entries.ToList().AsReadOnly();
		}

		public void Reset()
		{
			_entries.Clear();
		}
	}
}