using System;
using System.Collections.Generic;
using System.Linq;
using Kamsar.WebConsole;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Unicorn.Evaluators
{
	public static class EvaluatorUtility
	{
		public static void RecycleItems(Item[] items, IProgressStatus progress, Action<IProgressStatus, Item> deleteMessage)
		{
			Assert.ArgumentNotNull(items, "items");
			Assert.ArgumentNotNull(progress, "progress");

			Database db = items.First().Database;

			if(DoRecycleItems(items, progress, deleteMessage))
				db.Engines.TemplateEngine.Reset();
		}

		/// <summary>
		/// Deletes an item from Sitecore
		/// </summary>
		/// <returns>true if the item's database should have its template engine reloaded, false otherwise</returns>
		private static bool RecycleItem(Item item, IProgressStatus progress, Action<IProgressStatus, Item> deleteMessage)
		{
			bool resetFromChild = DoRecycleItems(item.Children, progress, deleteMessage);
			Database db = item.Database;
			ID id = item.ID;

			deleteMessage(progress, item);
			
			item.Recycle();

			if (EventDisabler.IsActive)
			{
				db.Caches.ItemCache.RemoveItem(id);
				db.Caches.DataCache.RemoveItemInformation(id);
			}

			if (!resetFromChild && item.Database.Engines.TemplateEngine.IsTemplatePart(item)) return true;

			return false;
		}

		/// <summary>
		/// Deletes a list of items. Ensures that obsolete cache data is also removed.
		/// </summary>
		/// <returns>
		/// Is set to <c>true</c> if template engine should reset afterwards.
		/// </returns>
		private static bool DoRecycleItems(IEnumerable<Item> items, IProgressStatus progress, Action<IProgressStatus, Item> deleteMessage)
		{
			bool reset = false;
			foreach (Item item in items)
			{
				if (RecycleItem(item, progress, deleteMessage))
					reset = true;
			}

			return reset;
		}
	}
}
