using System;
using System.IO;
using System.Text;
using Sitecore.StringExtensions;
using Unicorn.Logging;

namespace Unicorn.ControlPanel.Remote.Logging
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

		public void ReportSimple(string text, MessageLevel level)
		{
			SendOpeartionMessage(level, text);
		}

		public void Info(string message)
		{
			SendOpeartionMessage(MessageLevel.Info, message);
		}

		public void Debug(string message)
		{
			SendOpeartionMessage(MessageLevel.Debug, message);
		}

		public void Warn(string message)
		{
			SendOpeartionMessage(MessageLevel.Warning, message);
		}

		public void Error(string message)
		{
			SendOpeartionMessage(MessageLevel.Error, message);
		}

		public void Error(Exception exception)
		{
			SendOpeartionMessage(MessageLevel.Error, exception.ToString());
		}

		public void Flush()
		{
			
		}

		public void ReportProgress(int progress)
		{
			SendMessage(ReportType.Progress, MessageLevel.Info, progress.ToString());
		}

		private void SendOpeartionMessage(MessageLevel level, string message)
		{
			SendMessage(ReportType.Operation, level, message);
		}

		private void SendMessage(ReportType type, MessageLevel level, string message)
		{
			var encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
			var report = "{0}|{1}|{2}".FormatWith(type, level, encodedMessage);
			_output.WriteLine(report);
			_output.Flush();
		}
	}
}