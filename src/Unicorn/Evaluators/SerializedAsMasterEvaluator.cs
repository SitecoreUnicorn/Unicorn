using System;
using System.Collections.Generic;
using System.Linq;
using Gibson.Deserialization;
using Gibson.Model;
using Gibson.Predicates;
using Sitecore.Diagnostics;
using Unicorn.ControlPanel;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Predicates;

namespace Unicorn.Evaluators
{
	/// <summary>
	/// Evaluates to overwrite the source data if ANY differences exist in the serialized version.
	/// </summary>
	public class SerializedAsMasterEvaluator : IEvaluator, IDocumentable
	{
		private readonly ISerializedAsMasterEvaluatorLogger _logger;
		private readonly IFieldPredicate _fieldPredicate;
		private readonly ISourceDataStore _sourceDataStore;
		private readonly IDeserializer _deserializer;
		protected static readonly Guid RootId = new Guid("{11111111-1111-1111-1111-111111111111}");

		public SerializedAsMasterEvaluator(ISerializedAsMasterEvaluatorLogger logger, IFieldPredicate fieldPredicate, ISourceDataStore sourceDataStore, IDeserializer deserializer)
		{
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(fieldPredicate, "fieldPredicate");

			_logger = logger;
			_fieldPredicate = fieldPredicate;
			_sourceDataStore = sourceDataStore;
			_deserializer = deserializer;
		}

		public void EvaluateOrphans(ISerializableItem[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");

			EvaluatorUtility.RecycleItems(orphanItems, _sourceDataStore, item => _logger.DeletedItem(item));
		}

		public ISerializableItem EvaluateNewSerializedItem(ISerializableItem newItem)
		{
			Assert.ArgumentNotNull(newItem, "newItem");

			_logger.DeserializedNewItem(newItem);

			var updatedItem = DoDeserialization(newItem);

			return updatedItem;
		}

		public ISerializableItem EvaluateUpdate(ISerializableItem serializedItem, ISerializableItem existingItem)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(existingItem, "existingItem");

			var deferredUpdateLog = new DeferredLogWriter<ISerializedAsMasterEvaluatorLogger>();

			if (ShouldUpdateExisting(serializedItem, existingItem, deferredUpdateLog))
			{
				_logger.SerializedUpdatedItem(serializedItem);

				deferredUpdateLog.ExecuteDeferredActions(_logger);

				var updatedItem = DoDeserialization(serializedItem);

				return updatedItem;
			}

			return null;
		}

		protected virtual bool ShouldUpdateExisting(ISerializableItem serializedItem, ISerializableItem existingItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(existingItem, "existingItem");

			if (existingItem.Id == RootId) return false; // we never want to update the Sitecore root item

			// check if templates are different
			if (IsTemplateMatch(existingItem, serializedItem, deferredUpdateLog)) return true;

			// check if names are different
			if (IsNameMatch(existingItem, serializedItem, deferredUpdateLog)) return true;

			// check if source has version(s) that serialized does not
			var orphanVersions = existingItem.Versions.Where(sourceVersion => serializedItem.GetVersion(sourceVersion.Language.Name, sourceVersion.VersionNumber) == null).ToArray();
			if (orphanVersions.Length > 0)
			{
				deferredUpdateLog.AddEntry(x => x.OrphanSourceVersion(existingItem, serializedItem, orphanVersions));
				return true; // source contained versions not present in the serialized version, which is a difference
			}

			// check if shared fields have any mismatching values
			if (AnyFieldMatch(serializedItem.SharedFields, existingItem.SharedFields, existingItem, serializedItem, deferredUpdateLog))
				return true;

			// see if the serialized versions have any mismatching values in the source data
			return serializedItem.Versions.Any(serializedeVersion =>
			{
				var sourceISerializableVersion = existingItem.GetVersion(serializedeVersion.Language.Name, serializedeVersion.VersionNumber);

				// version exists in serialized item but does not in source version
				if (sourceISerializableVersion == null)
				{
					deferredUpdateLog.AddEntry(x => x.NewSerializedVersionMatch(serializedeVersion, serializedItem, existingItem));
					return true;
				}

				// field values mismatch
				var fieldMatch = AnyFieldMatch(serializedeVersion.Fields, sourceISerializableVersion.Fields, existingItem, serializedItem, deferredUpdateLog, serializedeVersion);
				if (fieldMatch) return true;

				// if we get here everything matches to the best of our knowledge, so we return false (e.g. "do not update this item")
				return false;
			});
		}

		protected virtual bool IsNameMatch(ISerializableItem existingItem, ISerializableItem serializedItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			if (!serializedItem.Name.Equals(existingItem.Name))
			{
				deferredUpdateLog.AddEntry(x => x.IsNameMatch(serializedItem, existingItem));

				return true;
			}

			return false;
		}

		protected virtual bool IsTemplateMatch(ISerializableItem existingItem, ISerializableItem serializedItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			if (existingItem.TemplateId == default(Guid) && serializedItem.TemplateId == default(Guid)) return false;

			bool match = !serializedItem.TemplateId.Equals(existingItem.TemplateId);
			if(match)
				deferredUpdateLog.AddEntry(x=>x.IsTemplateMatch(serializedItem, existingItem));

			return match;
		}

		protected virtual bool AnyFieldMatch(IEnumerable<ISerializableFieldValue> sourceFields, IEnumerable<ISerializableFieldValue> targetFields, ISerializableItem existingItem, ISerializableItem serializedItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog, ISerializableVersion version = null)
		{
			if (sourceFields == null) return false;
			var targetFieldIndex = targetFields.ToDictionary(x => x.FieldId);

			return sourceFields.Any(x =>
			{
				if (!_fieldPredicate.Includes(x.FieldId).IsIncluded) return false;

				if (!x.IsFieldComparable()) return false;

				bool isMatch = IsFieldMatch(x.Value, targetFieldIndex, x.FieldId);
				if(isMatch) deferredUpdateLog.AddEntry(logger =>
				{
					ISerializableFieldValue sourceFieldValue = targetFieldIndex[x.FieldId];

					if (version == null) logger.IsSharedFieldMatch(serializedItem, x.FieldId, x.Value, sourceFieldValue.Value);
					else logger.IsVersionedFieldMatch(serializedItem, version, x.FieldId, x.Value, sourceFieldValue.Value);
				});
				return isMatch;
			});
		}

		protected virtual bool IsFieldMatch(string sourceFieldValue, Dictionary<Guid, ISerializableFieldValue> targetFields, Guid fieldId)
		{
			// note that returning "true" means the values DO NOT MATCH EACH OTHER.

			if (sourceFieldValue == null) return false;

			// it's a "match" if the target item does not contain the source field
			ISerializableFieldValue targetFieldValue;
			if (!targetFields.TryGetValue(fieldId, out targetFieldValue)) return true;

			return !sourceFieldValue.Equals(targetFieldValue.Value);
		}

		protected virtual ISerializableItem DoDeserialization(ISerializableItem serializedItem)
		{
			ISerializableItem updatedItem = _deserializer.Deserialize(serializedItem, false);

			Assert.IsNotNull(updatedItem, "Do not return null from DeserializeItem() - throw an exception if an error occurs.");

			return updatedItem;
		}

		public string FriendlyName
		{
			get { return "Serialized as Master Evaluator"; }
		}

		public string Description
		{
			get { return "Treats the items that are serialized as the master copy, and any changes whether newer or older are synced into the source data. This allows for all merging to occur in source control, and is the default way Unicorn behaves."; }
		}

		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return null;
		}
	}
}
