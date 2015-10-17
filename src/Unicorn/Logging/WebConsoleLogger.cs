using System;
using System.Diagnostics.CodeAnalysis;
using System.Web;
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

		protected bool Quiet
		{
			get
			{
				if (HttpContext.Current == null) return false;
				return HttpContext.Current.Request.QueryString["quiet"] == "1";
			}
		}

		public WebConsoleLogger(IProgressStatus progress)
		{
			_progress = progress;
		}

		public void Info(string message)
		{
			if (Quiet) return;

			_progress.ReportStatus(message, MessageType.Info);
		}

		public void Debug(string message)
		{
			if (Quiet) return;

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
