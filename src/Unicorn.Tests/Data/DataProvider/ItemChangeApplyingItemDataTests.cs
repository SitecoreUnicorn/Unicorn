using System;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Sitecore.Data.Items;
using Unicorn.Data.DataProvider;
using Xunit;

namespace Unicorn.Tests.Data.DataProvider
{
	public class ItemChangeApplyingItemDataTests
	{
		[Theory]
		[AutoDbData]
		public void ShouldPreserveBaseItemSharedFields(Item item)
		{
			var proxy = new ProxyItem(new ItemData(item));
			proxy.SharedFields = new[] { new ProxyFieldValue(Guid.Empty, "hello") };

			var changes = new ItemChanges(item);
			var sut = new ItemChangeApplyingItemData(proxy, changes);

			sut.SharedFields.First().Value.Should().Be("hello");
		}

		[Theory]
		[AutoDbData]
		public void ShouldPreserveBaseItemVersionedFields(Item item)
		{
			var proxy = new ProxyItem(new ItemData(item));

			proxy.Versions = new[]
			{
				new ProxyItemVersion(new CultureInfo("en"), 1)
				{
					Fields = new[] { new ProxyFieldValue(Guid.Empty, "hello") }
				}
			};

			var changes = new ItemChanges(item);
			var sut = new ItemChangeApplyingItemData(proxy, changes);

			sut.Versions.First().Fields.First().Value.Should().Be("hello");
		}
	}
}
