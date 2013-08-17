using System;
using System.Linq;
using Kamsar.WebConsole;
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
		/// <param name="progress">Progress object to write status to</param>
		/// <param name="deleteMessage">The status message to write for each deleted item</param>
		public static void RecycleItems(ISourceItem[] items, IProgressStatus progress, Action<IProgressStatus, ISourceItem> deleteMessage)
		{
			Assert.ArgumentNotNull(items, "items");
			Assert.ArgumentNotNull(progress, "progress");

			foreach (var item in items)
				RecycleItem(item, progress, deleteMessage);
		}

		/// <summary>
		/// Deletes an item from the source data provider
		/// </summary>
		private static void RecycleItem(ISourceItem item, IProgressStatus progress, Action<IProgressStatus, ISourceItem> deleteMessage)
		{
			RecycleItems(item.Children, progress, deleteMessage);
			
			deleteMessage(progress, item);
			
			item.Recycle();
		}
	}
}
