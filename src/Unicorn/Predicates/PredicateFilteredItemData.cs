using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Unicorn.Predicates
{
	public class PredicateFilteredItemData : ItemDecorator
	{
		private readonly IPredicate _predicate;

		public PredicateFilteredItemData(IItemData innerItem, IPredicate predicate) : base(innerItem)
		{
			Assert.ArgumentNotNull(predicate, "predicate");

			_predicate = predicate;
		}

		public override IEnumerable<IItemData> GetChildren()
		{
			return base.GetChildren().Where(child => _predicate.Includes(child).IsIncluded);
		}
	}
}
