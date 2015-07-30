using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Rainbow.Model;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	/// <summary>
	/// Collects data from the evaluator logger that is used to generate metrics as to item processing
	/// </summary>
	public class DefaultSyncCompleteDataCollector: ISyncCompleteDataCollector
	{
		private readonly Queue<ChangeEntry> _entries = new Queue<ChangeEntry>();
 
		public void PushChangedItem(IItemData serializedItemData, ChangeType type)
		{
			_entries.Enqueue(new ChangeEntry(serializedItemData, type));
		}

		public ReadOnlyCollection<ChangeEntry> GetChanges()
		{
			return _entries.ToList().AsReadOnly();
		}

		public void AddProcessedItem()
		{
			ProcessedItemCount++;
		}

		public int ProcessedItemCount { get; private set; }

		public void Reset()
		{
			_entries.Clear();
			ProcessedItemCount = 0;
		}
	}
}