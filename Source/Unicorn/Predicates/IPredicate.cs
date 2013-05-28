using Sitecore.Data.Items;
using Unicorn.Serialization;

namespace Unicorn.Predicates
{
	public interface IPredicate
	{
		string Name { get; }
		PredicateResult Includes(Item item);
		PredicateResult Includes(ISerializedReference item);

		void SerializeAll(ISerializationProvider provider);
		Item[] GetRootItems();
	}
}
