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
		/// Fired when an item's name is different between serialized and source
		/// </summary>
		void IsNameMatch(ISerializedItem serializedItem, ISourceItem existingItem);

		/// <summary>
		/// Fired when an item's template is different between serialized and source
		/// </summary>
		void IsTemplateMatch(ISerializedItem serializedItem, ISourceItem existingItem);

		/// <summary>
		/// Fired when a shared field value is different between serialized and source
		/// </summary>
		/// <remarks>Note that the sourceValue may be null</remarks>
		void IsSharedFieldMatch(ISerializedItem serializedItem, string fieldName, string serializedValue, string sourceValue);

		/// <summary>
		/// Fired when a version's field value is different between serialized and source
		/// </summary>
		/// <remarks>Note that the sourceValue may be null</remarks>
		void IsVersionedFieldMatch(ISerializedItem serializedItem, ItemVersion version, string fieldName, string serializedValue, string sourceValue);

		/// <summary>
		/// Fired when a later version is found in the serialized version of an item
		/// </summary>
		void NewSerializedVersionMatch(ItemVersion newSerializedVersion, ISerializedItem serializedItem, ISourceItem existingItem);

		/// <summary>
		/// Called when you serialize an item that does not yet exist in the provider
		/// </summary>
		void DeserializedNewItem(ISerializedItem serializedItem);

		/// <summary>
		/// Called when you serialize an updated item that already existed in the provider
		/// </summary>
		void SerializedUpdatedItem(ISerializedItem serializedItem);

		/// <summary>
		/// Called when extra versions exist in a source item compared to the serialized version
		/// </summary>
		void OrphanSourceVersion(ISourceItem existingItem, ISerializedItem serializedItem, ItemVersion[] orphanSourceVersions);
	}
}
