using System;
using System.Linq;
using System.Text;

namespace Unicorn.Logging
{
	public class ExceptionFormatter
	{
		public string FormatExceptionAsHtml(Exception exception)
		{
			var aggregateException = exception as DeserializationAggregateException;

			if (aggregateException != null) return FormatAggregateExceptionAsHtml(aggregateException);

			var exMessage = new StringBuilder();
			exMessage.AppendFormat("ERROR: {0} ({1})", exception.Message, exception.GetType().FullName);
			exMessage.Append("<div class=\"stacktrace\">");

			if (exception.StackTrace != null)
				exMessage.Append(exception.StackTrace.Trim().Replace("\n", "<br />"));
			else
				exMessage.Append("No stack trace available.");

			exMessage.Append("</div>");

			WriteInnerExceptionAsHtml(exception.InnerException, exMessage);

			return exMessage.ToString();
		}

		public string FormatExceptionAsText(Exception exception)
		{
			var exMessage = new StringBuilder();
			exMessage.AppendFormat("ERROR: {0} ({1})", exception.Message, exception.GetType().FullName);
			exMessage.AppendLine();

			if (exception.StackTrace != null)
				exMessage.Append(exception.StackTrace.Trim());
			else
				exMessage.Append("No stack trace available.");

			exMessage.AppendLine();

			WriteInnerExceptionAsText(exception.InnerException, exMessage);

			return exMessage.ToString();
		}

		private static string FormatAggregateExceptionAsHtml(DeserializationAggregateException exception)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("ERROR: {0} unrecoverable errors occurred during deserialization. Note that due to error retrying, some of these items may have appeared to 'load' earlier, but they have not.<br />", exception.InnerExceptions.Length);
			sb.Append(string.Join("<br />", exception.InnerExceptions.Select(x =>
			{
				if (x.InnerException != null && x.InnerException is DeserializationException)
					return x.InnerException.Message;

				return x.Message;
			})));
			sb.Append("<br />For full stack traces of each exception, see the Sitecore logs.");

			return sb.ToString();
		}

		private static void WriteInnerExceptionAsText(Exception innerException, StringBuilder exMessage)
		{
			if (innerException == null) return;

			exMessage.AppendLine("INNER EXCEPTION");
			exMessage.AppendFormat("{0} ({1})", innerException.Message, innerException.GetType().FullName);
			exMessage.AppendLine();

			if (innerException.StackTrace != null)
				exMessage.Append(innerException.StackTrace.Trim());
			else
				exMessage.Append("No stack trace available.");

			WriteInnerExceptionAsText(innerException.InnerException, exMessage);

			exMessage.AppendLine();
		}

		private static void WriteInnerExceptionAsHtml(Exception innerException, StringBuilder exMessage)
		{
			if (innerException == null) return;

			exMessage.Append("<div class=\"innerexception\">");
			exMessage.AppendFormat("{0} ({1})", innerException.Message, innerException.GetType().FullName);
			exMessage.Append("<div class=\"stacktrace\">");

			if (innerException.StackTrace != null)
				exMessage.Append(innerException.StackTrace.Trim().Replace("\n", "<br />"));
			else
				exMessage.Append("No stack trace available.");

			WriteInnerExceptionAsHtml(innerException.InnerException, exMessage);

			exMessage.Append("</div>");
		}
	}
}
