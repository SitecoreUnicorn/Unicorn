using Kamsar.WebConsole;
using Sitecore.Data.Items;
namespace Unicorn.Serialization
{
	public interface ISerializationProvider
	{
		void SerializeSingleItem(Item item);
		Item DeserializeItem(ISerializedItem serializedItem, IProgressStatus progress);

		ISerializedReference GetReference(string sitecorePath, string databaseName);
		ISerializedItem GetItem(ISerializedReference reference);
		ISerializedReference[] GetChildReferences(ISerializedReference parent);
		ISerializedItem[] GetChildItems(ISerializedReference parent);
	}
}
