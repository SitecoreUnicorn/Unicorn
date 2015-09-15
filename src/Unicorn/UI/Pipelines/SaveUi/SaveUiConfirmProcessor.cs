using Sitecore.Diagnostics;
using Sitecore.Pipelines.Save;
using Sitecore.Web.UI.Sheer;

namespace Unicorn.UI.Pipelines.SaveUi
{
	/// <summary>
	/// Base class for a SaveUI processor that pops up a confirmation dialog that can abort saving
	/// </summary>
	public abstract class SaveUiConfirmProcessor
	{
		public void Process(SaveArgs args)
		{
			Assert.ArgumentNotNull(args, "args");

			if (!args.HasSheerUI)
			{
				return;
			}

			// we had errors, and we got a post-back result of no, don't overwrite
			if (args.Result == "no" || args.Result == "undefined")
			{
				args.SaveAnimation = false;
				args.AbortPipeline();
				return;
			}

			// we had errors, and we got a post-back result of yes, allow overwrite
			if (args.IsPostBack) return;

			string error = GetDialogText(args);

			// no errors detected, we're good
			if (string.IsNullOrEmpty(error)) return;
			
			SheerResponse.Confirm(error);
			args.WaitForPostBack();
		}

		protected abstract string GetDialogText(SaveArgs args);
	}
}
