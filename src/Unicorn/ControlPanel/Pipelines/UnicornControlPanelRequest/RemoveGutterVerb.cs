using System;
using Kamsar.WebConsole;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using Unicorn.Logging;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class RemoveGutterVerb : GutterVerbBase
	{
		public const string VerbName = "RemoveGutter";
		public RemoveGutterVerb() : base(VerbName, "Remove Gutter Addon")
		{
		}
		protected override void Process(IProgressStatus progress, ILogger additionalLogger)
		{
			Item gutterItem = GetGutterItem();

			if (gutterItem == null)
			{
				additionalLogger.Warn("JOB COMPLETE. Gutter icon does not exist. Aborting...");
				WebConsoleUtility.SetTaskProgress(progress, 1, 1, 100);
				return;
			}
			using (new SecurityDisabler())
			{
				try
				{
					gutterItem.Delete();
					additionalLogger.Info("JOB COMPLETE. Gutter item deleted successfully!");
					WebConsoleUtility.SetTaskProgress(progress, 1, 1, 100);
				}
				catch (Exception ex)
				{
					additionalLogger.Error(ex);
					additionalLogger.Error("JOB FAILED. An error has occurred. See above stack trace for details.");
					WebConsoleUtility.SetTaskProgress(progress, 1, 1, 100);
				}
			}
		}
	}
}
