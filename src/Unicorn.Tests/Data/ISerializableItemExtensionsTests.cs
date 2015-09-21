using Rainbow.Tests;
using Unicorn.Data;
using Xunit;

namespace Unicorn.Tests.Data
{
	public class SerializableItemExtensionsTests
	{
		[Fact]
		public void ShouldReturnExpectedValue()
		{
			var fake = new FakeItem();

			Assert.Equal("master:/sitecore/content/test item (" + fake.Id + ")", fake.GetDisplayIdentifier());
		}
	}
}
