using System;
using System.Diagnostics;
using System.Text;
using Kamsar.WebConsole;

namespace Unicorn.ControlPanel
{
	class UnicornStringConsole : IProgressStatus
	{
		readonly StringBuilder _output = new StringBuilder();
		private bool _hasErrors = false;
		int _progressPercent;

		public void ReportException(Exception exception)
		{
			var exMessage = new StringBuilder();
			exMessage.AppendFormat("ERROR: {0} ({1})", exception.Message, exception.GetType().FullName);
			exMessage.AppendLine();

			if (exception.StackTrace != null)
				exMessage.Append(exception.StackTrace.Trim());
			else
				exMessage.Append("No stack trace available.");

			exMessage.AppendLine();

			WriteInnerException(exception.InnerException, exMessage);

			ReportStatus(exMessage.ToString(), MessageType.Error);

			if (Debugger.IsAttached) Debugger.Break();
		}

		public void ReportStatus(string statusMessage, params object[] formatParameters)
		{
			ReportStatus(statusMessage, MessageType.Info, formatParameters);
		}

		public void ReportStatus(string statusMessage, MessageType type, params object[] formatParameters)
		{
			var line = new StringBuilder();

			line.AppendFormat("{0}: ", type);

			if (formatParameters.Length > 0)
				line.AppendFormat(statusMessage, formatParameters);
			else
				line.Append(statusMessage);

			_output.AppendLine(line.ToString());

			if (type == MessageType.Error) _hasErrors = true;
		}

		public void Report(int percent)
		{
			_progressPercent = percent;
		}

		public void ReportTransientStatus(string statusMessage, params object[] formatParameters)
		{
			// do nothing
		}

		/// <summary>
		/// All available console output
		/// </summary>
		public string Output => _output.ToString();

		public bool HasErrors => _hasErrors;

		public int Progress => _progressPercent;

		private static void WriteInnerException(Exception innerException, StringBuilder exMessage)
		{
			if (innerException == null) return;

			exMessage.AppendLine("INNER EXCEPTION");
			exMessage.AppendFormat("{0} ({1})", innerException.Message, innerException.GetType().FullName);
			exMessage.AppendLine();

			if (innerException.StackTrace != null)
				exMessage.Append(innerException.StackTrace.Trim());
			else
				exMessage.Append("No stack trace available.");

			WriteInnerException(innerException.InnerException, exMessage);

			exMessage.AppendLine();
		}
	}
}
