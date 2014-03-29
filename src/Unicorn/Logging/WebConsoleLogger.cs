using System;
using Kamsar.WebConsole;

namespace Unicorn.Logging
{
	/// <summary>
	/// Logger that writes to a WebConsole.
	/// </summary>
	public class WebConsoleLogger : ILogger
	{
		private readonly IProgressStatus _progress;

		public WebConsoleLogger(IProgressStatus progress)
		{
			_progress = progress;
		}

		public void Info(string message)
		{
			_progress.ReportStatus(message, MessageType.Info);
		}

		public void Debug(string message)
		{
			_progress.ReportStatus(message, MessageType.Debug);
		}

		public void Warn(string message)
		{
			_progress.ReportStatus(message, MessageType.Warning);
		}

		public void Error(string message)
		{
			_progress.ReportStatus(message, MessageType.Error);
		}

		public void Error(Exception exception)
		{
			_progress.ReportStatus(new ExceptionFormatter().FormatExceptionAsHtml(exception), MessageType.Error);
		}
	}
}
