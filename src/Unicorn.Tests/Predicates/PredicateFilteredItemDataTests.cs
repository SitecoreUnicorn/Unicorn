using System;
using NSubstitute;
using Rainbow.Model;
using Unicorn.Predicates;
using Xunit;

namespace Unicorn.Tests.Predicates
{
	public class PredicateFilteredItemDataTests
	{
		[Fact]
		public void ShouldExcludeChildrenExcludedByPredicate()
		{
			var child = new ProxyItem();
			var parent = new ProxyItem();
			parent.SetProxyChildren(new[] { child });

			var predicate = Substitute.For<IPredicate>();
			predicate.Includes(child).Returns(new PredicateResult(false));

			Assert.NotEmpty(parent.GetChildren());

			var filtered = new PredicateFilteredItemData(parent, predicate);

			Assert.Empty(filtered.GetChildren());
		}
	}
}
