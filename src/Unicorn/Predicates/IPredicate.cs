using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Predicates
{
	public interface IPredicate
	{
		string Name { get; }
		PredicateResult Includes(ISourceItem item);
		PredicateResult Includes(ISerializedReference item);

		ISourceItem[] GetRootItems();
	}
}
