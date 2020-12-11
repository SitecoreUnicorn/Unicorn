using Kamsar.WebConsole;
using System;

namespace Unicorn.ControlPanel.SyncSilentClasses
{
	public class SyncSilentProgressStatus : IProgressStatus
	{
		public int Progress { get; private set; }

		public void Report(int percent)
		{
			Progress = percent;
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
			switch (type)
			{
				case MessageType.Info:
					break;
				case MessageType.Debug:
					break;
				case MessageType.Warning:
					break;
				case MessageType.Error:
					HasErrors = true;
					break;
			}
		}

		public void ReportTransientStatus(string statusMessage, params object[] formatParameters)
		{
			//do nothing
		}

		public bool HasErrors { get; private set; }
	}
}
