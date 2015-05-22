using System;
using System.Linq;
using Gibson.Model;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncComplete;

namespace Unicorn.Evaluators
{
	public class DefaultSerializedAsMasterEvaluatorLogger : ISerializedAsMasterEvaluatorLogger
	{
		private readonly ILogger _logger;
		private readonly ISyncCompleteDataCollector _pipelineDataCollector;
		private readonly ISourceDataStore _sourceDataStore;
		private const int MaxFieldLenthToDisplayValue = 40;

		public DefaultSerializedAsMasterEvaluatorLogger(ILogger logger, ISyncCompleteDataCollector pipelineDataCollector, ISourceDataStore sourceDataStore)
		{
			_logger = logger;
			_pipelineDataCollector = pipelineDataCollector;
			_sourceDataStore = sourceDataStore;
		}

		public virtual void DeletedItem(ISerializableItem deletedItem)
		{
			_logger.Warn("[D] {0} because it did not exist in the serialization provider.".FormatWith(deletedItem.GetDisplayIdentifier()));
			_pipelineDataCollector.PushChangedItem(deletedItem, ChangeType.Deleted);
		}

		public virtual void IsSharedFieldMatch(ISerializableItem serializedItem, Guid fieldId, string serializedValue, string sourceValue)
		{
			if (serializedValue.Length < MaxFieldLenthToDisplayValue && (sourceValue == null || sourceValue.Length < MaxFieldLenthToDisplayValue))
			{
				_logger.Debug("> Field {0} - Serialized {1}, Source {2}".FormatWith(TryResolveItemName(serializedItem.DatabaseName, fieldId), serializedValue, sourceValue));
			}
			else
			{
				_logger.Debug("> Field {0} - Value mismatch (values too long to display)".FormatWith(TryResolveItemName(serializedItem.DatabaseName, fieldId)));
			}
		}

		public virtual void IsVersionedFieldMatch(ISerializableItem serializedItem, ISerializableVersion version, Guid fieldId, string serializedValue, string sourceValue)
		{
			if (serializedValue.Length < MaxFieldLenthToDisplayValue && (sourceValue == null || sourceValue.Length < MaxFieldLenthToDisplayValue))
			{
				_logger.Debug("> Field {0} - {1}#{2}: Serialized {3}, Source {4}".FormatWith(TryResolveItemName(serializedItem.DatabaseName, fieldId), version.Language, version.VersionNumber, serializedValue, sourceValue));
			}
			else
			{
				_logger.Debug("> Field {0} - {1}#{2}: Value mismatch (values too long to display)".FormatWith(TryResolveItemName(serializedItem.DatabaseName, fieldId), version.Language, version.VersionNumber));
			}
		}

		public virtual void IsTemplateMatch(ISerializableItem serializedItem, ISerializableItem existingItem)
		{
			_logger.Debug("> Template: Serialized \"{0}\", Source \"{1}\"".FormatWith(TryResolveItemName(serializedItem.DatabaseName, serializedItem.TemplateId), TryResolveItemName(existingItem.DatabaseName, existingItem.TemplateId)));
		}

		public virtual void IsNameMatch(ISerializableItem serializedItem, ISerializableItem existingItem)
		{
			_logger.Debug("> Name: Serialized \"{0}\", Source \"{1}\"".FormatWith(serializedItem.Name, existingItem.Name));
		}


		public virtual void NewSerializedVersionMatch(ISerializableVersion newSerializedVersion, ISerializableItem serializedItem, ISerializableItem existingItem)
		{
			_logger.Debug("> New version {0}#{1} (serialized)".FormatWith(newSerializedVersion.Language, newSerializedVersion.VersionNumber));
		}

		public virtual void OrphanSourceVersion(ISerializableItem existingItem, ISerializableItem serializedItem, ISerializableVersion[] orphanSourceVersions)
		{
			_logger.Debug("> Orphaned version{0} {1} (source)".FormatWith(orphanSourceVersions.Length > 1 ? "s" : string.Empty, string.Join(", ", orphanSourceVersions.Select(x => x.Language + "#" + x.VersionNumber))));
		}

		public virtual void DeserializedNewItem(ISerializableItem serializedItem)
		{
			_logger.Info("[A] {0}".FormatWith(serializedItem.GetDisplayIdentifier()));
			_pipelineDataCollector.PushChangedItem(serializedItem, ChangeType.Created);
		}

		public virtual void SerializedUpdatedItem(ISerializableItem serializedItem)
		{
			_logger.Info("[U] {0}".FormatWith(serializedItem.GetDisplayIdentifier()));
			_pipelineDataCollector.PushChangedItem(serializedItem, ChangeType.Modified);
		}

		protected virtual string TryResolveItemName(string database, Guid fieldId)
		{
			var fieldItem = _sourceDataStore.GetById(database, fieldId);
			if (fieldItem != null) return fieldItem.Name;

			return fieldId.ToString();
		}
	}
}
