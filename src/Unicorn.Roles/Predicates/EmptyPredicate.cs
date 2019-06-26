using System.Collections.Generic;
using Rainbow.Model;
using Rainbow.Storage;
using Unicorn.Predicates;

namespace Unicorn.Roles.Predicates
{
	/// <summary>
	/// A predicate that includes no items at all. Used to allow security-only predicates.
	/// </summary>
	public class EmptyPredicate : IPredicate, ITreeRootFactory
	{
		public string FriendlyName => "Empty Predicate";
		public string Description => "Includes no items at all. Used for role-only configurations.";

		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new KeyValuePair<string, string>[0];
		}

		public PredicateResult Includes(IItemData itemData)
		{
			return new PredicateResult(false);
		}

		public TreeRoot[] GetRootPaths()
		{
			return new TreeRoot[0];
		}

		public IEnumerable<TreeRoot> CreateTreeRoots()
		{
			yield break;
		}
	}
}
