using Kamsar.WebConsole;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public class ConsoleSerializedAsMasterEvaluatorLogger : ISerializedAsMasterEvaluatorLogger
	{
		private readonly IProgressStatus _progress;

		public ConsoleSerializedAsMasterEvaluatorLogger(IProgressStatus progress)
		{
			_progress = progress;
		}

		public void DeletedItem(ISourceItem deletedItem)
		{
			_progress.ReportStatus("[D] {0} because it did not exist in the serialization provider.".FormatWith(deletedItem.DisplayIdentifier), MessageType.Warning);
		}


		public void CannotEvaluateUpdate(ISerializedItem serializedItem, SerializedVersion version)
		{
			_progress.ReportStatus("{0} ({1} #{2}): Serialized version had no modified or revision field to check for update.", MessageType.Warning, serializedItem.ItemPath, version.Language, version.VersionNumber);
		}


		public void IsModifiedMatch(ISerializedItem serializedItem, SerializedVersion version, System.DateTime serializedModified, System.DateTime itemModified)
		{
			_progress.ReportStatus("{0} ({1} #{2}): Disk modified {3}, Item modified {4}", MessageType.Debug, serializedItem.ItemPath, version.Language, version.VersionNumber, serializedModified.ToString("G"), itemModified.ToString("G"));
		}


		public void IsRevisionMatch(ISerializedItem serializedItem, SerializedVersion version, string serializedRevision, string itemRevision)
		{
			_progress.ReportStatus(string.Format("{0} ({1} #{2}): Disk revision {3}, Item revision {4}", serializedItem.ItemPath, version.Language, version.VersionNumber, serializedRevision, itemRevision), MessageType.Debug);
		}


		public void IsNameMatch(ISerializedItem serializedItem, ISourceItem existingItem, SerializedVersion version)
		{
			_progress.ReportStatus(string.Format("{0} ({1} #{2}): Disk name {3}, Item name {4}", serializedItem.ItemPath, version.Language, version.VersionNumber, serializedItem.Name, existingItem.Name), MessageType.Debug);
		}
	}
}
