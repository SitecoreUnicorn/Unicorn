using System;
using System.Management.Automation;
using System.Management.Automation.Host;
using Kamsar.WebConsole;

namespace Unicorn.PowerShell
{
	public class PowershellProgressStatus : IProgressStatus
	{
		private readonly PSHost _host;
		private readonly string _progressActivity;
		private const long SourceId = 31337;
		private const int ActivityId = 1337;
		private string _currentTransientStatus = string.Empty;

		public PowershellProgressStatus(PSHost host, string progressActivity)
		{
			_host = host;
			_progressActivity = progressActivity;
		}
		public void Report(int percent)
		{
			Progress = percent;
			if (string.IsNullOrWhiteSpace(_currentTransientStatus))
			{
				_currentTransientStatus = $"{Progress}% complete.";
			}

			_host.UI.WriteProgress(SourceId, new ProgressRecord(ActivityId, _progressActivity, _currentTransientStatus) { PercentComplete = percent });
		}

		public void ReportException(Exception exception)
		{
			ReportStatus(exception.ToString(), MessageType.Error);
		}

		public void ReportStatus(string statusMessage, params object[] formatParameters)
		{
			ReportStatus(statusMessage, MessageType.Info, formatParameters);
		}

		public void ReportStatus(string statusMessage, MessageType type, params object[] formatParameters)
		{
			var message = string.Format(statusMessage, formatParameters);

			switch (type)
			{
				case MessageType.Info:
					_host.UI.WriteLine(message);
					break;
				case MessageType.Debug:
					_host.UI.WriteDebugLine(message);
					break;
				case MessageType.Warning:
					_host.UI.WriteWarningLine(message);
					break;
				case MessageType.Error:
					_host.UI.WriteErrorLine(message);
					HasErrors = true;
					break;
			}
		}

		public void ReportTransientStatus(string statusMessage, params object[] formatParameters)
		{
			_currentTransientStatus = string.Format(statusMessage, formatParameters);
			Report(Progress);
		}

		public int Progress { get; private set; }

		public bool HasErrors { get; private set; }
	}
}
