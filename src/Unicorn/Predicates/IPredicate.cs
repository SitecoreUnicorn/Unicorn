using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Predicates
{
	/// <summary>
	/// The predicate defines where loading should start (root items) and whether items should be included
	/// </summary>
	public interface IPredicate
	{
		string Name { get; }
		PredicateResult Includes(ISourceItem item);
		PredicateResult Includes(ISerializedReference item);

		PredicateRootPath[] GetRootPaths();
	}
}
