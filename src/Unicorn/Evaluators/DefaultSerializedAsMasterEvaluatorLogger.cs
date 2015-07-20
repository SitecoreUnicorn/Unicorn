using System;
using System.Linq;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Data;
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
		private const int MaxFieldLenthToDisplayValue = 40;

		public DefaultSerializedAsMasterEvaluatorLogger(ILogger logger, ISyncCompleteDataCollector pipelineDataCollector)
		{
			_logger = logger;
			_pipelineDataCollector = pipelineDataCollector;
		}

		public virtual void DeletedItem(IItemData deletedItem)
		{
			_logger.Warn("[D] {0} because it did not exist in the serialization provider.".FormatWith(deletedItem.GetDisplayIdentifier()));
			_pipelineDataCollector.PushChangedItem(deletedItem, ChangeType.Deleted);
		}

		public virtual void SharedFieldIsChanged(IItemData targetItem, Guid fieldId, string serializedValue, string sourceValue)
		{
			if (serializedValue.Length < MaxFieldLenthToDisplayValue && (sourceValue == null || sourceValue.Length < MaxFieldLenthToDisplayValue))
			{
				_logger.Debug("> Field {0} - Serialized {1}, Source {2}".FormatWith(TryResolveItemName(targetItem.DatabaseName, fieldId), serializedValue, sourceValue));
			}
			else
			{
				_logger.Debug("> Field {0} - Value mismatch (values too long to display)".FormatWith(TryResolveItemName(targetItem.DatabaseName, fieldId)));
			}
		}

		public virtual void IsVersionedFieldMatch(IItemData serializedItemData, IItemVersion version, Guid fieldId, string serializedValue, string sourceValue)
		{
			if (serializedValue.Length < MaxFieldLenthToDisplayValue && (sourceValue == null || sourceValue.Length < MaxFieldLenthToDisplayValue))
			{
				_logger.Debug("> Field {0} - {1}#{2}: Serialized {3}, Source {4}".FormatWith(TryResolveItemName(serializedItemData.DatabaseName, fieldId), version.Language, version.VersionNumber, serializedValue, sourceValue));
			}
			else
			{
				_logger.Debug("> Field {0} - {1}#{2}: Value mismatch (values too long to display)".FormatWith(TryResolveItemName(serializedItemData.DatabaseName, fieldId), version.Language, version.VersionNumber));
			}
		}

		public virtual void TemplateChanged(IItemData sourceItem, IItemData targetItem)
		{
			_logger.Debug("> Template: Serialized \"{0}\", Source \"{1}\"".FormatWith(TryResolveItemName(targetItem.DatabaseName, targetItem.TemplateId), TryResolveItemName(sourceItem.DatabaseName, sourceItem.TemplateId)));
		}

		public virtual void Renamed(IItemData sourceItem, IItemData targetItem)
		{
			_logger.Debug("> Name: Serialized \"{0}\", Source \"{1}\"".FormatWith(targetItem.Name, sourceItem.Name));
		}


		public virtual void NewSerializedVersionMatch(IItemVersion newSerializedVersion, IItemData serializedItemData, IItemData existingItemData)
		{
			_logger.Debug("> New version {0}#{1} (serialized)".FormatWith(newSerializedVersion.Language, newSerializedVersion.VersionNumber));
		}

		public virtual void OrphanSourceVersion(IItemData existingItemData, IItemData serializedItemData, IItemVersion[] orphanSourceVersions)
		{
			_logger.Debug("> Orphaned version{0} {1} (source)".FormatWith(orphanSourceVersions.Length > 1 ? "s" : string.Empty, string.Join(", ", orphanSourceVersions.Select(x => x.Language + "#" + x.VersionNumber))));
		}

		public virtual void DeserializedNewItem(IItemData serializedItemData)
		{
			_logger.Info("[A] {0}".FormatWith(serializedItemData.GetDisplayIdentifier()));
			_pipelineDataCollector.PushChangedItem(serializedItemData, ChangeType.Created);
		}

		public virtual void SerializedUpdatedItem(IItemData serializedItemData)
		{
			_logger.Info("[U] {0}".FormatWith(serializedItemData.GetDisplayIdentifier()));
			_pipelineDataCollector.PushChangedItem(serializedItemData, ChangeType.Modified);
		}

		protected virtual string TryResolveItemName(string database, Guid fieldId)
		{
			var db = Factory.GetDatabase(database);

			if (db != null)
			{
				var fieldItem = db.GetItem(new ID(fieldId));

				if (fieldItem != null) return fieldItem.Name;
			}

			return fieldId.ToString();
		}
	}
}
