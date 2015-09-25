using System;
using System.Globalization;
using System.Timers;
using System.Web;
using System.Web.UI;
using Kamsar.WebConsole;
using Sitecore.SecurityModel;
using Unicorn.ControlPanel.Controls;
using Unicorn.ControlPanel.Headings;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Creates a Html5WebConsole to render something into, using Unicorn chrome
	/// 
	/// Note that classes using this need to use LoggingContext/WebConsoleLogger to attach the WebConsole
	/// to the current Unicorn logger instance or they will not receive Unicorn log output.
	/// </summary>
	public abstract class ControlPanelConsole : IControlPanelControl
	{
		private readonly bool _isAutomatedTool;
		private readonly HeadingService _headingService;

		protected ControlPanelConsole(bool isAutomatedTool, HeadingService headingService)
		{
			_isAutomatedTool = isAutomatedTool;
			_headingService = headingService;
		}

		protected abstract string Title { get; }

		public void Render(HtmlTextWriter writer)
		{
			if (_isAutomatedTool)
			{
				var console = new StringProgressStatus();
				ProcessInternal(console);

				HttpContext.Current.Response.ContentType = "text/plain";
				HttpContext.Current.Response.Write(Title + "\n\n");
				HttpContext.Current.Response.Write(console.Output);

				if (console.HasErrors)
				{
					HttpContext.Current.Response.StatusCode = 500;
					HttpContext.Current.Response.TrySkipIisCustomErrors = true;
				}

				HttpContext.Current.Response.End();
			}
			else
			{
				var console = new CustomStyledHtml5WebConsole(HttpContext.Current.Response);
				console.Title = Title;
				console.Render(ProcessInternal);
			}
		}

		protected virtual void ProcessInternal(IProgressStatus progress)
		{
			if (_headingService != null && !_isAutomatedTool)
			{
				progress.ReportStatus(_headingService.GetHeadingHtml());
			}

			// note: these logs are intentionally to progress and not loggingConsole as we don't need them in the Sitecore logs

			progress.ReportTransientStatus("Executing.");

			var heartbeat = new Timer(3000);
			var startTime = DateTime.Now;
			heartbeat.AutoReset = true;
			heartbeat.Elapsed += (sender, args) =>
			{
				var elapsed = Math.Round((args.SignalTime - startTime).TotalSeconds);

				try
				{
					progress.ReportTransientStatus("Executing for {0} sec.", elapsed.ToString(CultureInfo.InvariantCulture));
				}
				catch
				{
					// e.g. HTTP connection disconnected - prevent infinite looping
					heartbeat.Stop();
				}
			};

			heartbeat.Start();

			try
			{
				using (new SecurityDisabler())
				{
					Process(progress);
				}
			}
			finally
			{
				heartbeat.Stop();
			}

			progress.Report(100);
			progress.ReportTransientStatus("Completed.");
			progress.ReportStatus(_isAutomatedTool ? "\r\n" : "<br>");
			progress.ReportStatus("Completed. Want to <a href=\"?verb=\">return to the control panel?</a>");
		}

		protected abstract void Process(IProgressStatus progress);

		/// <summary>
		/// Sets the progress of the whole based on the progress within a sub-task of the main progress (e.g. 0-100% of a task within the global range of 0-20%)
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="taskNumber">The index of the current sub-task</param>
		/// <param name="totalTasks">The total number of sub-tasks</param>
		/// <param name="taskPercent">The percentage complete of the sub-task (0-100)</param>
		protected void SetTaskProgress(IProgressStatus progress, int taskNumber, int totalTasks, int taskPercent)
		{
			if (taskNumber < 1) throw new ArgumentException("taskNumber must be 1 or more");
			if (totalTasks < 1) throw new ArgumentException("totalTasks must be 1 or more");
			if (taskNumber > totalTasks) throw new ArgumentException("taskNumber was greater than the number of totalTasks!");

			int start = (int)Math.Round(((taskNumber - 1) / (double)totalTasks) * 100d);
			int end = start + (int)Math.Round((1d / totalTasks) * 100d);

			SetRangeTaskProgress(progress, Math.Max(start, 0), Math.Min(end, 100), taskPercent);
		}

		/// <summary>
		/// Sets the progress of the whole based on the progress within a percentage range of the main progress (e.g. 0-100% of a task within the global range of 0-20%)
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="startPercentage">The percentage the task began at</param>
		/// <param name="endPercentage">The percentage the task ends at</param>
		/// <param name="taskPercent">The percentage complete of the sub-task (0-100)</param>
		private void SetRangeTaskProgress(IProgressStatus progress, int startPercentage, int endPercentage, int taskPercent)
		{
			int range = endPercentage - startPercentage;

			if (range <= 0) throw new ArgumentException("endPercentage must be greater than startPercentage");

			int offset = (int)Math.Round(range * (taskPercent / 100d));

			progress.Report(Math.Min(startPercentage + offset, 100));
		}

		private class CustomStyledHtml5WebConsole : Html5WebConsole
		{
			private readonly HttpResponse _response;
			private readonly object _writeLock = new object();

			public CustomStyledHtml5WebConsole(HttpResponse response) : base(response)
			{
				_response = response;
			}

			protected override void RenderPageHead()
			{
				_response.Write(new HeadingService().GetControlPanelHeadingHtml());
			}

			public override void WriteScript(string script)
			{
				lock (_writeLock)
				{
					base.WriteScript(script);
				}
			}

			public override void RenderResources()
			{
				base.RenderResources();
				_response.Write("<link href='https://fonts.googleapis.com/css?family=Source+Sans+Pro:400,700,400italic' rel='stylesheet' type='text/css'>");
				_response.Write(@"<style>
					.wrapper { width: auto; max-width: 1850px; } 
					a, a:visited { color: lightblue; } 
					#console{ height: 40em; } 
					h1 { font-size: 3em; } 
					h1, p { font-family: 'Source Sans Pro'; } 
					.line { margin: 1em 0 0 0; font-size: 1.2em; display: block; }
					.line-inner { display: block; font-size: 0.9em; }
					.line-smaller { font-size: 0.7em !important; margin-bottom: .4em; font-style: italic; color: gray; }
				</style>");
			}
		}
	}
}
