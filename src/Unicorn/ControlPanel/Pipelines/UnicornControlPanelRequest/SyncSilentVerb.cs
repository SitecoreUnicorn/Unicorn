using Kamsar.WebConsole;
using Unicorn.ControlPanel.Responses;
using Unicorn.ControlPanel.SyncSilentClasses;
using Unicorn.Logging;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	/// <summary>
	/// This verb is used for silent sync via Unicorn
	/// </summary>
	public class SyncSilentVerb : SyncVerb
	{
		private SyncStatus Status;
		private SyncSilentProgressStatus Progress { get; set; }
		// lost possibility to define log level for logger in query string; it's a pity :(
		private SyncSilentLogger Logger { get; set; }

		public SyncSilentVerb() : base("SyncSilent", new SerializationHelper())
		{
			Reset();
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			const string SyncInProgress = "Sync in progress";
			if (Status == SyncStatus.NotStarted)
			{
				var configurations = ResolveConfigurations();
				System.Threading.Tasks.Task.Run(() => ProcessAsync(Progress, Logger, configurations))
					.ContinueWith(t =>
					{
						if (t.Exception != null)
						{
							Status = SyncStatus.Finished;
							Progress.ReportException(t.Exception);
						}
					});
				return new PlainTextResponse(SyncInProgress);
			}

			if (Status == SyncStatus.Started)
			{
				return new PlainTextResponse(SyncInProgress);
			}

			string message = Logger.LogData.ToString();

			//resetting sync data
			Reset();
			// in any case, need to send 200 OK here and parse response then in script
			return new PlainTextResponse(message);
		}

		private async void ProcessAsync(IProgressStatus progress, ILogger additionalLogger, Configuration.IConfiguration[] configurations)
		{
			Status = SyncStatus.Started;
			_helper.SyncConfigurations(configurations, progress, additionalLogger);
			Status = SyncStatus.Finished;
		}

		private void Reset()
		{
			Progress = new SyncSilentProgressStatus();
			Logger = new SyncSilentLogger(Progress);
			Status = SyncStatus.NotStarted;
		}
	}
}
