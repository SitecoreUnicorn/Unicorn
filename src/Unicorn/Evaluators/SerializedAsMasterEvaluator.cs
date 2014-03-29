using System.Linq;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Unicorn.ControlPanel;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Predicates;
using Unicorn.Serialization;
using System.Collections.Generic;

namespace Unicorn.Evaluators
{
	/// <summary>
	/// Evaluates to overwrite the source data if ANY differences exist in the serialized version.
	/// </summary>
	public class SerializedAsMasterEvaluator : IEvaluator, IDocumentable
	{
		private readonly ISerializedAsMasterEvaluatorLogger _logger;
		private readonly IFieldPredicate _fieldPredicate;
		protected static readonly ID RootId = new ID("{11111111-1111-1111-1111-111111111111}");

		public SerializedAsMasterEvaluator(ISerializedAsMasterEvaluatorLogger logger, IFieldPredicate fieldPredicate)
		{
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(fieldPredicate, "fieldPredicate");

			_logger = logger;
			_fieldPredicate = fieldPredicate;
		}

		public void EvaluateOrphans(ISourceItem[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");

			EvaluatorUtility.RecycleItems(orphanItems, item => _logger.DeletedItem(item));
		}

		public ISourceItem EvaluateNewSerializedItem(ISerializedItem newItem)
		{
			Assert.ArgumentNotNull(newItem, "newItem");

			_logger.DeserializedNewItem(newItem);

			var updatedItem = DoDeserialization(newItem);

			return updatedItem;
		}

		public ISourceItem EvaluateUpdate(ISerializedItem serializedItem, ISourceItem existingItem)
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

		protected virtual bool ShouldUpdateExisting(ISerializedItem serializedItem, ISourceItem existingItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(existingItem, "existingItem");

			if (existingItem.Id == RootId) return false; // we never want to update the Sitecore root item

			// check if templates are different
			if (IsTemplateMatch(existingItem, serializedItem, deferredUpdateLog)) return true;

			// check if names are different
			if (IsNameMatch(existingItem, serializedItem, deferredUpdateLog)) return true;

			// check if source has version(s) that serialized does not
			var orphanVersions = existingItem.Versions.Where(sourceItemVersion => serializedItem.GetVersion(sourceItemVersion.Language, sourceItemVersion.VersionNumber) == null).ToArray();
			if (orphanVersions.Length > 0)
			{
				deferredUpdateLog.AddEntry(x => x.OrphanSourceVersion(existingItem, serializedItem, orphanVersions));
				return true; // source contained versions not present in the serialized version, which is a difference
			}

			// check if shared fields have any mismatching values
			if (AnyFieldMatch(serializedItem.SharedFields, existingItem.SharedFields, existingItem, serializedItem, deferredUpdateLog))
				return true;

			// see if the serialized versions have any mismatching values in the source data
			return serializedItem.Versions.Any(serializedItemVersion =>
			{
				var sourceItemVersion = existingItem.GetVersion(serializedItemVersion.Language, serializedItemVersion.VersionNumber);

				// version exists in serialized item but does not in source version
				if (sourceItemVersion == null)
				{
					deferredUpdateLog.AddEntry(x => x.NewSerializedVersionMatch(serializedItemVersion, serializedItem, existingItem));
					return true;
				}

				// field values mismatch
				var fieldMatch = AnyFieldMatch(serializedItemVersion.Fields, sourceItemVersion.Fields, existingItem, serializedItem, deferredUpdateLog, serializedItemVersion);
				if (fieldMatch) return true;

				// if we get here everything matches to the best of our knowledge, so we return false (e.g. "do not update this item")
				return false;
			});
		}

		protected virtual bool IsNameMatch(ISourceItem existingItem, ISerializedItem serializedItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			if (!serializedItem.Name.Equals(existingItem.Name))
			{
				deferredUpdateLog.AddEntry(x => x.IsNameMatch(serializedItem, existingItem));

				return true;
			}

			return false;
		}

		protected virtual bool IsTemplateMatch(ISourceItem existingItem, ISerializedItem serializedItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog)
		{
			if (existingItem.TemplateId == (ID) null && serializedItem.TemplateId == (ID) null) return false;

			bool match = !serializedItem.TemplateId.Equals(existingItem.TemplateId);
			if(match)
				deferredUpdateLog.AddEntry(x=>x.IsTemplateMatch(serializedItem, existingItem));

			return match;
		}

		protected virtual bool AnyFieldMatch(FieldDictionary sourceFields, FieldDictionary targetFields, ISourceItem existingItem, ISerializedItem serializedItem, DeferredLogWriter<ISerializedAsMasterEvaluatorLogger> deferredUpdateLog, ItemVersion version = null)
		{
			if (sourceFields == null) return false;

			return sourceFields.Any(x =>
			{
				if (!_fieldPredicate.Includes(x.Key).IsIncluded) return false;

				if (!existingItem.IsFieldComparable(x.Key)) return false;

				bool isMatch = IsFieldMatch(x.Value, targetFields, x.Key);
				if(isMatch) deferredUpdateLog.AddEntry(logger =>
				{
					string sourceFieldValue;
					targetFields.TryGetValue(x.Key, out sourceFieldValue);

					if (version == null) logger.IsSharedFieldMatch(serializedItem, x.Key, x.Value, sourceFieldValue);
					else logger.IsVersionedFieldMatch(serializedItem, version, x.Key, x.Value, sourceFieldValue);
				});
				return isMatch;
			});
		}

		protected virtual bool IsFieldMatch(string sourceFieldValue, FieldDictionary targetFields, string fieldId)
		{
			// note that returning "true" means the values DO NOT MATCH EACH OTHER.

			if (sourceFieldValue == null) return false;

			// it's a "match" if the target item does not contain the source field
			string targetFieldValue;
			if (!targetFields.TryGetValue(fieldId, out targetFieldValue)) return true;

			return !sourceFieldValue.Equals(targetFieldValue);
		}

		protected virtual ISourceItem DoDeserialization(ISerializedItem serializedItem)
		{
			ISourceItem updatedItem = serializedItem.Deserialize(false);

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
