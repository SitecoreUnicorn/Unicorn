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
 
		public void PushChangedItem(ISerializableItem serializedItem, ChangeType type)
		{
			_entries.Enqueue(new ChangeEntry(serializedItem, type));
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