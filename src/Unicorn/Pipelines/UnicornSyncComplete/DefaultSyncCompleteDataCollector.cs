using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	/// <summary>
	/// Collects data from the evaluator logger that is used to generate metrics as to item processing
	/// </summary>
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

		public ReadOnlyCollection<ChangeEntry> GetChanges()
		{
			return _entries.ToList().AsReadOnly();
		}

		public void Reset()
		{
			_entries.Clear();
		}
	}
}