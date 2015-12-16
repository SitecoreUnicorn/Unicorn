namespace Unicorn.Predicates
{
	public interface IPresetTreeExclusion
	{
		PredicateResult Evaluate(string itemPath);
		string Description { get; }
	}
}
