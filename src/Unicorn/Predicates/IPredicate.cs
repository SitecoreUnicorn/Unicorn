using Gibson.Model;
using Gibson.Predicates;

namespace Unicorn.Predicates
{
	/// <summary>
	/// The predicate defines where loading should start (root items) and whether items should be included
	/// </summary>
	public interface IPredicate
	{
		string Name { get; }
		PredicateResult Includes(ISerializableItem item);

		PredicateRootPath[] GetRootPaths();
	}
}
