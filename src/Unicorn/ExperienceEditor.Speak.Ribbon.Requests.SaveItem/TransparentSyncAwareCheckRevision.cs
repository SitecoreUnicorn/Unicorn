using System;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;
using Sitecore.Pipelines.Save;
using Sitecore.Shell;
using Unicorn.Data.DataProvider;

namespace Unicorn.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
	// Prevent fake 'overwrite?' warnings when using transparent sync due to how it handles revisions
	public class TransparentSyncAwareCheckRevision : PipelineProcessorRequest<PageContext>
	{
		public override PipelineProcessorResponseValue ProcessRequest()
		{
			SaveArgs.SaveItem saveItem = RequestContext.GetSaveArgs().Items[0];
			PipelineProcessorResponseValue processorResponseValue = new PipelineProcessorResponseValue();

			Item existingItem = RequestContext.Item.Database.GetItem(saveItem.ID, Language.Parse(RequestContext.Language), Sitecore.Data.Version.Parse(RequestContext.Version));

			if (existingItem == null)
			{
				return processorResponseValue;
			}

			// added: ignore transparent synced items, whose revisions are randomly generated
			if (existingItem.Statistics.UpdatedBy.Equals(UnicornDataProvider.TransparentSyncUpdatedByValue, StringComparison.Ordinal))
			{
				return processorResponseValue;
			}

			string cleanedExistingRevision = existingItem[FieldIDs.Revision].Replace("-", string.Empty);

			if (saveItem.Revision == string.Empty)
				saveItem.Revision = cleanedExistingRevision;

			string cleanedNewRevision = saveItem.Revision.Replace("-", string.Empty);

			if (cleanedExistingRevision.Equals(cleanedNewRevision, StringComparison.InvariantCultureIgnoreCase) || EditorConstants.IgnoreRevision.Equals(cleanedNewRevision, StringComparison.InvariantCultureIgnoreCase))
				return processorResponseValue;

			processorResponseValue.ConfirmMessage = Translate.Text("One or more items have been changed.\n\nDo you want to overwrite these changes?");

			return processorResponseValue;
		}
	}
}