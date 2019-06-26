using System.Linq;
using System.Text.RegularExpressions;
using Rainbow.Model;

namespace Unicorn.Predicates.Exclusions
{
	/// <summary>
	/// Excludes items with a given name, using regex
	/// NOTE: children of items whose name matches the pattern are also excluded
	/// NOTE: regex is case-insensitive (as Sitecore names are case-insensitive)
	/// e.g. <exclude namePattern="^__Standard values$" /> in config
	/// </summary>
	public class NameBasedPresetExclusion : IPresetTreeExclusion
	{
		private readonly string _namePattern;

		public NameBasedPresetExclusion(string namePattern)
		{
			_namePattern = namePattern;
		}

		public string Description => $"items whose names match: {_namePattern}";

		public PredicateResult Evaluate(IItemData itemData)
		{
			var nameCandidates = itemData.Path.Split('/').Reverse();

			foreach (var nameCandidate in nameCandidates)
			{
				if (Regex.IsMatch(nameCandidate, _namePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase))
				{
					return new PredicateResult($"Item name exclusion rule: {_namePattern}");
				}
			}

			return new PredicateResult(true);
		}
	}
}