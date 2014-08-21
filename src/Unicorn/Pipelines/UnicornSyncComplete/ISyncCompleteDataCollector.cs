using System.Collections.Generic;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	// Note: class can expect to be a singleton instance within a configuration
	public interface ISyncCompleteDataCollector
	{
		void PushChangedItem(ISerializedItem serializedItem, ChangeType type);
		void PushChangedItem(ISourceItem sourceItem, ChangeType type);

		IReadOnlyCollection<ChangeEntry> GetChanges();
		void Reset();
	}
}
