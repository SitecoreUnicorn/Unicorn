using System.Linq;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public class DefaultSerializedAsMasterEvaluatorLogger : ISerializedAsMasterEvaluatorLogger
	{
		private readonly ILogger _logger;

		public DefaultSerializedAsMasterEvaluatorLogger(ILogger logger)
		{
			_logger = logger;
		}

		public void DeletedItem(ISourceItem deletedItem)
		{
			_logger.Warn("[D] {0} because it did not exist in the serialization provider.".FormatWith(deletedItem.DisplayIdentifier));
		}


		public void CannotEvaluateUpdate(ISerializedItem serializedItem, ItemVersion version)
		{
			_logger.Warn("{0} ({1} #{2}): Serialized version had no modified or revision field to check for update.".FormatWith(serializedItem.ItemPath, version.Language, version.VersionNumber));
		}


		public void IsModifiedMatch(ISerializedItem serializedItem, ItemVersion version, System.DateTime serializedModified, System.DateTime itemModified)
		{
			_logger.Debug("> Modified - {0}#{1}: Serialized {2}, Source {3}".FormatWith(version.Language, version.VersionNumber, serializedModified.ToString("G"), itemModified.ToString("G")));
		}


		public void IsRevisionMatch(ISerializedItem serializedItem, ItemVersion version, string serializedRevision, string itemRevision)
		{
			_logger.Debug("> Revision - {0}#{1}: Serialized {2}, Source {3}".FormatWith(version.Language, version.VersionNumber, serializedRevision, itemRevision));
		}


		public void IsNameMatch(ISerializedItem serializedItem, ISourceItem existingItem, ItemVersion version)
		{
			_logger.Debug("> Name - {0}#{1}: Serialized \"{2}\", Source \"{3}\"".FormatWith(version.Language, version.VersionNumber, serializedItem.Name, existingItem.Name));
		}


		public void NewSerializedVersionMatch(ItemVersion newSerializedVersion, ISerializedItem serializedItem, ISourceItem existingItem)
		{
			_logger.Debug("> New version {0}#{1} (serialized)".FormatWith(newSerializedVersion.Language, newSerializedVersion.VersionNumber));
		}

		public void OrphanSourceVersion(ISourceItem existingItem, ISerializedItem serializedItem, ItemVersion[] orphanSourceVersions)
		{
			_logger.Debug("> Orphaned version{0} {1} (source)".FormatWith(orphanSourceVersions.Length > 1 ? "s" : string.Empty, string.Join(", ", orphanSourceVersions.Select(x => x.Language + "#" + x.VersionNumber))));
		}

		public void DeserializedNewItem(ISerializedItem serializedItem)
		{
			_logger.Info("[A] {0}".FormatWith(serializedItem.DisplayIdentifier));
		}

		public void SerializedUpdatedItem(ISerializedItem serializedItem)
		{
			_logger.Info("[U] {0}".FormatWith(serializedItem.DisplayIdentifier));
		}
	}
}
