using System.Collections.Concurrent;
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
		private readonly ConcurrentQueue<ChangeEntry> _entries = new ConcurrentQueue<ChangeEntry>();
 
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
			ChangeEntry entry;

			while (_entries.TryDequeue(out entry))
			{
				// this approximates Clear() for a ConcurrentQueue
			}

			ProcessedItemCount = 0;
		}
	}
}