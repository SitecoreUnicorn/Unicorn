using System;
using System.Diagnostics.CodeAnalysis;
using Kamsar.WebConsole;

namespace Unicorn.Logging
{
	/// <summary>
	/// Logger that writes to a WebConsole.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class WebConsoleLogger : ILogger
	{
		private readonly IProgressStatus _progress;
		private readonly MessageType _logLevel;

		public WebConsoleLogger(IProgressStatus progress, string logLevelValue)
		{
			_progress = progress;
			MessageType type;

			if(logLevelValue == null || !Enum.TryParse(logLevelValue, true, out type))
				type = MessageType.Debug;

			_logLevel = type;
		}

		public WebConsoleLogger(IProgressStatus progress, MessageType logLevel)
		{
			_progress = progress;
			_logLevel = logLevel;
		}

		public void Info(string message)
		{
			if (_logLevel == MessageType.Warning || _logLevel == MessageType.Error) return;

			_progress.ReportStatus(message, MessageType.Info);
		}

		public void Debug(string message)
		{
			if (_logLevel != MessageType.Debug) return;

			_progress.ReportStatus(message, MessageType.Debug);
		}

		public void Warn(string message)
		{
			if (_logLevel == MessageType.Error) return;

			_progress.ReportStatus(message, MessageType.Warning);
		}

		public void Error(string message)
		{
			_progress.ReportStatus(message, MessageType.Error);
		}

		public void Error(Exception exception)
		{
			var error = new ExceptionFormatter().FormatExceptionAsHtml(exception);

			if(exception is DeserializationSoftFailureAggregateException)
				_progress.ReportStatus(error, MessageType.Warning);
			else
				_progress.ReportStatus(error, MessageType.Error);
		}

		public void Flush()
		{
			
		}
	}
}
