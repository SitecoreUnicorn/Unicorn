using System.Linq;
using Kamsar.WebConsole;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public class ConsoleDuplicateIdConsistencyCheckerLogger : IDuplicateIdConsistencyCheckerLogger
	{
		private readonly IProgressStatus _progress;

		public ConsoleDuplicateIdConsistencyCheckerLogger(IProgressStatus progress)
		{
			_progress = progress;
		}

		public void DuplicateFound(ISerializedItem existingItem, ISerializedItem duplicateItem)
		{
			_progress.ReportStatus("Duplicate serialized item IDs were detected ({0}) - this usually indicates corruption in the serialization provider data.<br>Item 1: {1}<br> Item 1 ProviderId: {2}<br>Item 2: {3}<br>Item 2 ProviderId: {4}", MessageType.Error, existingItem.Id, existingItem.DisplayIdentifier, existingItem.ProviderId, duplicateItem.DisplayIdentifier, duplicateItem.ProviderId);
		}
	}
}
