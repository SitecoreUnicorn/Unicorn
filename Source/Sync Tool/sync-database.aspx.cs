using System;
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

		protected override void Process(WebConsole console)
		{
			// iterate through existing Sitecore items (eg includeentry.process)
			var presets = SerializationUtility.GetPreset();

			for (int i = 0; i < presets.Count; i++)
			{
				using (var progress = new WebConsoleProgressStatus("Syncing preset path " + new ItemReference(presets[i].Database, presets[i].Path), console, i+1, presets.Count))
				{
					ProcessPreset(presets[i], progress);
				}
			}
			
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
				progress.ReportException(ex);
			}
		}
	}
}