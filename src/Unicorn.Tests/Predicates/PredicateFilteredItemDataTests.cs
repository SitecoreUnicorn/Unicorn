using NSubstitute;
using Rainbow.Tests;
using Unicorn.Predicates;
using Xunit;

namespace Unicorn.Tests.Predicates
{
	public class PredicateFilteredItemDataTests
	{
		[Fact]
		public void ShouldExcludeChildrenExcludedByPredicate()
		{
			var child = new FakeItem();
			var parent = new FakeItem(children: new[] { child });
			var predicate = Substitute.For<IPredicate>();
			predicate.Includes(child).Returns(new PredicateResult(false));

			Assert.NotEmpty(parent.GetChildren());

			var filtered = new PredicateFilteredItemData(parent, predicate);

			Assert.Empty(filtered.GetChildren());
		}
	}
}
