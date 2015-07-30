using System.Collections.ObjectModel;
using Rainbow.Model;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	// Note: class can expect to be a singleton instance within a configuration
	public interface ISyncCompleteDataCollector
	{
		void PushChangedItem(IItemData itemData, ChangeType type);

		ReadOnlyCollection<ChangeEntry> GetChanges();
		void AddProcessedItem();
		int ProcessedItemCount { get; }
		void Reset();
	}
}
