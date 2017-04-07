using Rainbow.Model;

namespace Unicorn.Predicates
{
	public interface IPresetTreeExclusion
	{
		PredicateResult Evaluate(IItemData itemData);
		string Description { get; }
	}
}
