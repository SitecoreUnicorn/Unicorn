using Rainbow.Model;
using Unicorn.Data;
using Xunit;

namespace Unicorn.Tests.Data
{
	public class SerializableItemExtensionsTests
	{
		[Fact]
		public void ShouldReturnExpectedValue()
		{
			var fake = new ProxyItem { Path = "/sitecore/content/test item", DatabaseName = "master" };

			Assert.Equal("master:/sitecore/content/test item (" + fake.Id + ")", fake.GetDisplayIdentifier());
		}
	}
}
