using System;
using Kamsar.WebConsole;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.SecurityModel;
using Unicorn.Logging;
using Version = Sitecore.Data.Version;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class InstallGutterVerb : GutterVerbBase
	{
		public const string VerbName = "InstallGutter";
		private const string GutterFolderId = "{59F37069-3118-4151-8C01-5DA0EF12CB4E}";
		private const string GutterRendererTemplateId = "{F5D247E0-80E6-4F31-9921-D30D00B61B3C}";
		public InstallGutterVerb() : base(VerbName, "Install Gutter")
		{
		}

		protected override void Process(IProgressStatus progress, ILogger logger)
		{
			Item gutterItem = GetGutterItem();
			Database coredb = Factory.GetDatabase("core");

			if (gutterItem != null)
			{
				logger.Warn("JOB COMPLETE. Gutter icon exists. Aborting...");
				WebConsoleUtility.SetTaskProgress(progress, 1, 1, 100);
				return;
			}

			using (new SecurityDisabler())
			{
				Item gutterFolder = coredb.DataManager.DataEngine.GetItem(new ID(GutterFolderId), LanguageManager.DefaultLanguage,
					Version.Latest);

				logger.Info("Creating item");
				try
				{
					gutterItem = ItemManager.CreateItem("Transparent Sync", gutterFolder, new ID(GutterRendererTemplateId),
						new ID(GutterItemId));


					gutterItem.Editing.BeginEdit();
					gutterItem[FieldIDs.DisplayName] = "Transparent Sync";
					gutterItem["Header"] = "Transparent Sync";
					gutterItem["Type"] = "Unicorn.UI.Gutter.TransparentSyncGutter, Unicorn";
					gutterItem.Editing.EndEdit(true, false);

					logger.Info("JOB COMPLETE. Gutter item created successfully!");
					WebConsoleUtility.SetTaskProgress(progress, 1, 1, 100);
				}
				catch (Exception ex)
				{
					logger.Error(ex);
					logger.Error("JOB FAILED. An error has occurred. See above stack trace for details.");
					WebConsoleUtility.SetTaskProgress(progress, 1, 1, 100);
				}
			}
		}
	}
}
