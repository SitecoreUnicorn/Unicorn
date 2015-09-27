using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Unicorn.Predicates
{
	/// <summary>
	/// Wraps an item and changes the behavior of getting its children so that children ignored by an IPredicate are not returned
	/// Children are also wrapped by the filter, making it recursive
	/// </summary>
	public class PredicateFilteredItemData : ItemDecorator
	{
		private readonly IPredicate _predicate;

		public PredicateFilteredItemData(IItemData innerItem, IPredicate predicate) : base(innerItem)
		{
			Assert.ArgumentNotNull(innerItem, "innerItem");
			Assert.ArgumentNotNull(predicate, "predicate");

			_predicate = predicate;
		}

		public override IEnumerable<IItemData> GetChildren()
		{
			return base.GetChildren().Where(child => _predicate.Includes(child).IsIncluded).Select(child => new PredicateFilteredItemData(child, _predicate));
		}
	}
}
