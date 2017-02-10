using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.ContentEditor.Gutters;
using Unicorn.Data.DataProvider;

namespace Unicorn.UI.Gutter
{
	public class TransparentSyncGutter : GutterRenderer
	{
		protected override GutterIconDescriptor GetIconDescriptor(Item item)
		{
			Assert.ArgumentNotNull(item, "item");
			if (item.Statistics.UpdatedBy != UnicornDataProvider.TransparentSyncUpdatedByValue) return null;
			var gutterIconDescriptor = new GutterIconDescriptor
			{
				Icon = "Office/32x32/arrow_circle2.png",
				Tooltip = "This item is included by Unicorn Transparent Sync."
			};
			return gutterIconDescriptor;
		}
	}
}
