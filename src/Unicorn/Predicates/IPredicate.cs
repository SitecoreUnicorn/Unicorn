using Rainbow;
using Rainbow.Model;
using Rainbow.Storage;

namespace Unicorn.Predicates
{
	/// <summary>
	/// The predicate defines where loading should start (root items) and whether items should be included
	/// </summary>
	public interface IPredicate : IDocumentable
	{
		PredicateResult Includes(IItemData itemData);

		TreeRoot[] GetRootPaths();
	}
}
