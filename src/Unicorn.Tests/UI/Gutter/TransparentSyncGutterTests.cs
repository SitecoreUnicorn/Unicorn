using System;
using FluentAssertions;
using Ploeh.AutoFixture.Xunit2;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Shell.Applications.ContentEditor.Gutters;
using Unicorn.Data.DataProvider;
using Unicorn.Tests.TestingTools.Attributes;
using Unicorn.UI.Gutter;
using Xunit;

namespace Unicorn.Tests.UI.Gutter
{
	public class TransparentSyncGutterTests
	{
		[Theory, AutoSub]
		public void GetIconDescriptor_WhenItemIsNull_ThrowArgumentNullException([Greedy]TestableTransparentSyncGutter sut)
		{
			Assert.Throws<ArgumentNullException>(() => sut.Public_GetIconDescriptor(null));
		}

		[Theory, AutoDbData]
		public void GetIconDescriptor_WasLastUpdatedByTransparentSync_ReturnGutterIconDescriptor([Greedy]TestableTransparentSyncGutter sut, [Content] Item item)
		{
			using (new EditContext(item))
			{
				item[FieldIDs.UpdatedBy] = UnicornDataProvider.TransparentSyncUpdatedByValue;
				sut.Public_GetIconDescriptor(item).Should().BeAssignableTo<GutterIconDescriptor>();
			}
		}
	}
	public class TestableTransparentSyncGutter : TransparentSyncGutter
	{
		public GutterIconDescriptor Public_GetIconDescriptor(Item item)
		{
			return GetIconDescriptor(item);
		}
	}
}
