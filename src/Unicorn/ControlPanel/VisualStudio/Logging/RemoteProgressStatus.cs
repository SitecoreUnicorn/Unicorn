using System;
using System.IO;
using Kamsar.WebConsole;

namespace Unicorn.ControlPanel.VisualStudio.Logging
{
	internal class RemoteProgressStatus : IProgressStatus
	{
		private readonly StreamWriter _output;
		private int _percent;

		public RemoteProgressStatus(StreamWriter output)
		{
			_output = output;
		}

		public void Report(int percent)
		{
			_percent = percent;
			_output.SendMessage(ReportType.Progress, MessageLevel.Info, percent.ToString());
		}

		public void ReportException(Exception exception)
		{
		}

		public void ReportStatus(string statusMessage, params object[] formatParameters)
		{
		}

		public void ReportStatus(string statusMessage, MessageType type, params object[] formatParameters)
		{
		}

		public void ReportTransientStatus(string statusMessage, params object[] formatParameters)
		{
		}

		public int Progress => _percent;
	}
}