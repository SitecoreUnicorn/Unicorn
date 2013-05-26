using Sitecore.Data.Items;
using Unicorn.Serialization;

namespace Unicorn.Predicates
{
	public interface IPredicate
	{
		string Name { get; }
		PredicateResult Includes(Item item);
		PredicateResult Includes(ISerializedItem item);

		void SerializeAll(ISerializationProvider provider);
	}
}
