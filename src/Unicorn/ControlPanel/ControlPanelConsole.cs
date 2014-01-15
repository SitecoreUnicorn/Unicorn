using System;
using System.Globalization;
using System.Timers;
using System.Web;
using System.Web.UI;
using Kamsar.WebConsole;
using Sitecore.SecurityModel;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Creates a Html5WebConsole to render something into, using Unicorn chrome
	/// Also logs console output to the Sitecore log.
	/// </summary>
	public abstract class ControlPanelConsole : IControlPanelControl
	{
		protected abstract string Title { get; }

		public void Render(HtmlTextWriter writer)
		{
			var console = new CustomStyledHtml5WebConsole(HttpContext.Current.Response);
			console.Title = Title;

			console.Render(ProcessInternal);
		}

		protected virtual void ProcessInternal(IProgressStatus progress)
		{
			

			// this bad-ass ASCII art is from http://www.ascii-art.de/ascii/uvw/unicorn.txt - original credit to 'sk'
			const string unicorn = @"<pre>
                        /
                      .7
           \       , //
           |\.--._/|//
          /\ ) ) ).'/
         /(  \  // /       _   _ _   _ ___ ____ ___  ____  _   _ 
        /(   J`((_/ \     | | | | \ | |_ _/ ___/ _ \|  _ \| \ | |
       / ) | _\     /     | | | |  \| || | |  | | | | |_) |  \| |
      /|)  \  eJ    L     | |_| | |\  || | |__| |_| |  _ <| |\  |
     |  \ L \   L   L      \___/|_| \_|___\____\___/|_| \_\_| \_|
    /  \  J  `. J   L
    |  )   L   \/   \
   /  \    J   (\   /
  |  \      \   \```
</pre>";

			// note: these logs are intentionally to progress and not loggingConsole as we don't need them in the Sitecore logs
			progress.ReportStatus(unicorn, MessageType.Warning);
			progress.ReportTransientStatus("Executing.");

			var heartbeat = new Timer(3000);
			var startTime = DateTime.Now;
			heartbeat.AutoReset = true;
			heartbeat.Elapsed += (sender, args) =>
			{
				var elapsed = Math.Round((args.SignalTime - startTime).TotalSeconds);

				progress.ReportTransientStatus("Executing for {0} sec.", elapsed.ToString(CultureInfo.InvariantCulture));
			};

			heartbeat.Start();

			try
			{
				// log to both to the web console AND the Sitecore logs
				var loggingConsole = new TeeProgressStatus(progress, new SitecoreLogProgressStatus());
				using (new SecurityDisabler())
				{
					Process(loggingConsole);
				}
			}
			finally
			{
				heartbeat.Stop();
			}

			progress.Report(100);
			progress.ReportTransientStatus("Completed.");
			progress.ReportStatus("Completed. Want to <a href=\"?verb=\">return to the control panel?</a>");
		}

		protected abstract void Process(IProgressStatus progress);

		private class CustomStyledHtml5WebConsole : Html5WebConsole
		{
			private readonly HttpResponse _response;

			public CustomStyledHtml5WebConsole(HttpResponse response) : base(response)
			{
				_response = response;
			}

			public override void RenderResources()
			{
				base.RenderResources();
				_response.Write(@"<style>.wrapper { width: auto; max-width: 960px; } a, a:visited { color: lightblue; }</style>");
			}
		}
	}
}
