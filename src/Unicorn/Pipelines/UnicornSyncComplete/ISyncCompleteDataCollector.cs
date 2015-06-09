using System.Collections.ObjectModel;
using Rainbow.Model;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	// Note: class can expect to be a singleton instance within a configuration
	public interface ISyncCompleteDataCollector
	{
		void PushChangedItem(ISerializableItem item, ChangeType type);

		ReadOnlyCollection<ChangeEntry> GetChanges();
		void Reset();
	}
}
