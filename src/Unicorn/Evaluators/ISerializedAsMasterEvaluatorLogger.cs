using System;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public interface ISerializedAsMasterEvaluatorLogger
	{
		/// <summary>
		/// Called when an item is evaluated for deletion
		/// </summary>
		void DeletedItem(ISourceItem deletedItem);

		/// <summary>
		/// Called when an item cannot be evaluated for an update (e.g. it had no valid criteria to compare)
		/// </summary>
		void CannotEvaluateUpdate(ISerializedItem serializedItem, ItemVersion version);

		/// <summary>
		/// Fired when the Modified timestamp is different between serialized and source
		/// </summary>
		void IsModifiedMatch(ISerializedItem serializedItem, ItemVersion version, DateTime serializedModified, DateTime itemModified);

		/// <summary>
		/// Fired when a version's Revision GUID is different between serialized and source
		/// </summary>
		void IsRevisionMatch(ISerializedItem serializedItem, ItemVersion version, string serializedRevision, string itemRevision);

		/// <summary>
		/// Fired when an item's name is different between serialized and source
		/// </summary>
		void IsNameMatch(ISerializedItem serializedItem, ISourceItem existingItem, ItemVersion version);

		/// <summary>
		/// Fired when a later version is found in the serialized version of an item
		/// </summary>
		void NewSerializedVersionMatch(ItemVersion newSerializedVersion, ISerializedItem serializedItem, ISourceItem existingItem);
	}
}
