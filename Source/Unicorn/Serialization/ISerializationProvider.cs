using Sitecore.Data.Items;
namespace Unicorn.Serialization
{
	public interface ISerializationProvider
	{
		void SerializeSingleItem(Item item);
	}
}
