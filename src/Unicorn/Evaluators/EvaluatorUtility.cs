using System;
using System.Linq;
using Sitecore.Diagnostics;
using Unicorn.Data;

namespace Unicorn.Evaluators
{
	public static class EvaluatorUtility
	{
		/// <summary>
		/// Recycles a whole tree of items and reports their progress
		/// </summary>
		/// <param name="items">The item(s) to delete. Note that their children will be deleted before them, and also be reported upon.</param>
		/// <param name="deleteMessage">The status message to write for each deleted item</param>
		public static void RecycleItems(ISourceItem[] items, Action<ISourceItem> deleteMessage)
		{
			Assert.ArgumentNotNull(items, "items");

			foreach (var item in items)
				RecycleItem(item, deleteMessage);
		}

		/// <summary>
		/// Deletes an item from the source data provider
		/// </summary>
		private static void RecycleItem(ISourceItem item, Action<ISourceItem> deleteMessage)
		{
			RecycleItems(item.Children, deleteMessage);
			
			deleteMessage(item);
			
			item.Recycle();
		}
	}
}
