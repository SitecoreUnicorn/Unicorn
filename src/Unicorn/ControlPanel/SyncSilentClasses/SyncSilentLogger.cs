using Kamsar.WebConsole;
using System;
using System.Text;
using Unicorn.Logging;

namespace Unicorn.ControlPanel.SyncSilentClasses
{
	public class SyncSilentLogger : ILogger
	{
		public StringBuilder LogData
		{
			get; set;
		}
		private readonly IProgressStatus _progress;
		public SyncSilentLogger(IProgressStatus progress)
		{
			LogData = new StringBuilder();
			_progress = progress;
		}
		public void Debug(string message)
		{
			_progress.ReportStatus(message, MessageType.Debug);
			LogData.AppendLine($"Debug: {message}");
		}

		public void Error(string message)
		{
			_progress.ReportStatus(message, MessageType.Error);
			LogData.AppendLine($"Error: {message}");
		}

		public void Error(Exception exception)
		{
			var error = new ExceptionFormatter().FormatExceptionAsHtml(exception);

			if (exception is DeserializationSoftFailureAggregateException)
			{
				_progress.ReportStatus(error, MessageType.Warning);
				LogData.AppendLine($"Warn: {error}");
			}
			else
			{
				_progress.ReportStatus(error, MessageType.Error);
				LogData.AppendLine($"Error: {error}");
			}
		}

		public void Flush()
		{
		}

		public void Info(string message)
		{
			_progress.ReportStatus(message, MessageType.Info);
			LogData.AppendLine($"Info: {message}");
		}

		public void Warn(string message)
		{
			_progress.ReportStatus(message, MessageType.Warning);
			LogData.AppendLine($"Warn: {message}");
		}
	}
}
