using Kamsar.WebConsole;
using Sitecore.Data.Items;
namespace Unicorn.Serialization
{
	public interface ISerializationProvider
	{
		/// <summary>
		/// Serialize a given item into the serialization provider's serialization store
		/// </summary>
		void SerializeItem(Item item);

		/// <summary>
		/// Deserialize a given item from the provider's serialization store to Sitecore database
		/// </summary>
		Item DeserializeItem(ISerializedItem serializedItem, IProgressStatus progress);

		/// <summary>
		/// Get a serialized reference for a given Sitecore path and database. A reference is a pointer to the path,
		/// and while it indicates the store is capable of storing the path, it does not guarantee that path is serialized.
		/// </summary>
		ISerializedReference GetReference(string sitecorePath, string databaseName);

		/// <summary>
		/// Get a serialized item from a serialized reference. This dereferences a reference into an actual serialized item.
		/// Unlike a reference, this should return null if the reference path is not actually serialized.
		/// </summary>
		ISerializedItem GetItem(ISerializedReference reference);

		/// <summary>
		/// Get all child references for a given serialized reference parent
		/// </summary>
		ISerializedReference[] GetChildReferences(ISerializedReference parent);

		/// <summary>
		/// Get all child serialized items for a given serialized reference parent
		/// </summary>
		ISerializedItem[] GetChildItems(ISerializedReference parent);
	}
}
