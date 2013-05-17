using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kamsar.WebConsole;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;
using Sitecore.SecurityModel;
using Unicorn;

namespace Unicorn
{
	public partial class SyncDatabase : WebConsolePage
	{
		protected override string PageTitle
		{
			get { return "Serialization Sync Tool"; }
		}

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);
			
			// make sure you can actually run this page
			if (!IsAuthorized())
			{
				Response.StatusCode = 404;
				Response.End();
			}
		}

		protected override void Process(IProgressStatus progress)
		{
			// load the requested (or default) preset
			var presets = GetPresetName(progress);
			if (presets == null)
			{
				progress.ReportStatus("Preset did not exist in configuration.", MessageType.Error);
				return;
			}

			for (int i = 0; i < presets.Count; i++)
			{
				using (var subtask = new SubtaskProgressStatus("Syncing preset path " + new ItemReference(presets[i].Database, presets[i].Path), progress, i+1, presets.Count))
				{
					ProcessPreset(presets[i], subtask);
				}
			}
		}

		private IList<IncludeEntry> GetPresetName(IProgressStatus progress)
		{
			string presetName = Request.QueryString["preset"] ?? "default";

			progress.ReportStatus("Using preset name {0}", MessageType.Info, presetName);

			return SerializationUtility.GetPreset(presetName);
		}

		private bool IsAuthorized()
		{
			var user = AuthenticationManager.GetActiveUser();

			if (user.IsAdministrator)
				return true;

			var authToken = Request.Headers["Authenticate"];
			var correctAuthToken = ConfigurationManager.AppSettings["DeploymentToolAuthToken"];

			if (!string.IsNullOrWhiteSpace(correctAuthToken) && 
				!string.IsNullOrWhiteSpace(authToken) &&
				authToken.Equals(correctAuthToken, StringComparison.Ordinal))
				return true;

			return false;
		}

		private static void ProcessPreset(IncludeEntry preset, IProgressStatus progress)
		{
			try
			{
				using (new SecurityDisabler())
				{
					new SerializationLoader().LoadTree(
						new AdvancedLoadOptions(preset)
							{
								Progress = progress,
								ForceUpdate = false,
								DeleteOrphans = true
							});
				}
			}
			catch (Exception ex)
			{
				if(Debugger.IsAttached) Debugger.Break();
				progress.ReportException(ex);
			}
		}
	}
}