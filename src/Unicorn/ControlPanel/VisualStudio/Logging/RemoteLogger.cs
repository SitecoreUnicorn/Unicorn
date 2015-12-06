using System;
using System.IO;
using Unicorn.Logging;

namespace Unicorn.ControlPanel.VisualStudio.Logging
{
	/// <summary>
	/// Logger implementation used for streaming remote API (used by Visual Studio control panel)
	/// </summary>
	public class RemoteLogger : ILogger
	{
		private readonly StreamWriter _output;

		public RemoteLogger(StreamWriter output)
		{
			_output = output;
		}

		public void Info(string message)
		{
			SendOperationMessage(MessageLevel.Info, message);
		}

		public void Debug(string message)
		{
			SendOperationMessage(MessageLevel.Debug, message);
		}

		public void Warn(string message)
		{
			SendOperationMessage(MessageLevel.Warning, message);
		}

		public void Error(string message)
		{
			SendOperationMessage(MessageLevel.Error, message);
		}

		public void Error(Exception exception)
		{
			SendOperationMessage(MessageLevel.Error, exception.ToString());
		}

		public void Flush()
		{

		}

		public void ReportProgress(int progress)
		{
			SendMessage(ReportType.Progress, MessageLevel.Info, progress.ToString());
		}

		private void SendOperationMessage(MessageLevel level, string message)
		{
			SendMessage(ReportType.Operation, level, message);
		}

		private void SendMessage(ReportType type, MessageLevel level, string message)
		{
			_output.SendMessage(type, level, message);
		}
	}
}