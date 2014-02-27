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
		private const int MaxFieldLenthToDisplayValue = 40;

		public DefaultSerializedAsMasterEvaluatorLogger(ILogger logger)
		{
			_logger = logger;
		}

		public void DeletedItem(ISourceItem deletedItem)
		{
			_logger.Warn("[D] {0} because it did not exist in the serialization provider.".FormatWith(deletedItem.DisplayIdentifier));
		}

		public void IsSharedFieldMatch(ISerializedItem serializedItem, string fieldName, string serializedValue, string sourceValue)
		{
			if (serializedValue.Length < MaxFieldLenthToDisplayValue && (sourceValue == null || sourceValue.Length < MaxFieldLenthToDisplayValue))
			{
				_logger.Debug("> Field {0} - Serialized {1}, Source {2}".FormatWith(fieldName, serializedValue, sourceValue));
			}
			else
			{
				_logger.Debug("> Field {0} - Value mismatch (values too long to display)".FormatWith(fieldName));
			}
		}

		public void IsVersionedFieldMatch(ISerializedItem serializedItem, ItemVersion version, string fieldName, string serializedValue, string sourceValue)
		{
			if (serializedValue.Length < MaxFieldLenthToDisplayValue && (sourceValue == null || sourceValue.Length < MaxFieldLenthToDisplayValue))
			{
				_logger.Debug("> Field {0} - {1}#{2}: Serialized {3}, Source {4}".FormatWith(fieldName, version.Language, version.VersionNumber, serializedValue, sourceValue));
			}
			else
			{
				_logger.Debug("> Field {0} - {1}#{2}: Value mismatch (values too long to display)".FormatWith(fieldName, version.Language, version.VersionNumber));
			}
		}

		public void IsTemplateMatch(ISerializedItem serializedItem, ISourceItem existingItem)
		{
			_logger.Debug("> Template: Serialized \"{0}\", Source \"{1}\"".FormatWith(serializedItem.TemplateName, existingItem.TemplateName));
		}

		public void IsNameMatch(ISerializedItem serializedItem, ISourceItem existingItem)
		{
			_logger.Debug("> Name: Serialized \"{0}\", Source \"{1}\"".FormatWith(serializedItem.Name, existingItem.Name));
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
