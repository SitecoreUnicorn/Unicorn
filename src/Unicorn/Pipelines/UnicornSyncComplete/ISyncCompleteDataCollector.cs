using System.Collections.ObjectModel;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	// Note: class can expect to be a singleton instance within a configuration
	public interface ISyncCompleteDataCollector
	{
		void PushChangedItem(ISerializedItem serializedItem, ChangeType type);
		void PushChangedItem(ISourceItem sourceItem, ChangeType type);

		ReadOnlyCollection<ChangeEntry> GetChanges();
		void Reset();
	}
}
