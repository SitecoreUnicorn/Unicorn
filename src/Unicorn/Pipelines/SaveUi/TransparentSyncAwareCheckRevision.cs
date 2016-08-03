using System;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Save;
using Sitecore.Shell;
using Sitecore.Web.UI.Sheer;
using Unicorn.Data.DataProvider;

namespace Unicorn.Pipelines.SaveUi
{
	// Prevent fake 'overwrite?' warnings when using transparent sync due to how it handles revisions
	public class TransparentSyncAwareCheckRevision
	{
		public virtual void Process(SaveArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			if (args.IsPostBack)
			{
				if (args.Result == "no")
					args.AbortPipeline();

				args.IsPostBack = false;

				return;
			}

			if (args.Items == null)
				return;

			foreach (var saveItem in args.Items)
			{
				if (!string.IsNullOrEmpty(saveItem.Revision))
				{
					Item existingItem = Client.ContentDatabase.GetItem(saveItem.ID, saveItem.Language, saveItem.Version);
					if (existingItem == null) continue;

					// added: ignore transparent synced items, whose revisions are randomly generated
					if (existingItem.Statistics.UpdatedBy.Equals(UnicornDataProvider.TransparentSyncUpdatedByValue, StringComparison.Ordinal)) continue;

					string cleanedExistingRevision = existingItem[FieldIDs.Revision].Replace("-", string.Empty);
					string cleanedNewRevision = saveItem.Revision.Replace("-", string.Empty);
					if (cleanedExistingRevision.Equals(cleanedNewRevision, StringComparison.InvariantCultureIgnoreCase) || EditorConstants.IgnoreRevision.Equals(cleanedNewRevision, StringComparison.InvariantCultureIgnoreCase))
						continue;

					if (!args.HasSheerUI)
						break;

					SheerResponse.Confirm("One or more items have been changed.\n\nDo you want to overwrite these changes?");

					args.WaitForPostBack();

					break;
				}
			}
		}
	}
}