using System;
using System.Linq;
using System.Text;
using Rainbow.Storage.Sc.Deserialization;

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

			var aggregate = exception as DeserializationAggregateException;

			if (aggregate != null)
			{
				foreach(var inner in aggregate.InnerExceptions) WriteInnerExceptionAsText(inner, exMessage);
			}

			return exMessage.ToString();
		}

		private static string FormatAggregateExceptionAsHtml(DeserializationAggregateException exception)
		{
			var sb = new StringBuilder();

			var dedupedExceptions = exception.InnerExceptions
				.GroupBy(ex => ex.Message)
				.Select(group => group.First())
				.ToArray();

			sb.AppendFormat("ERROR: {0} unrecoverable error{1} occurred during deserialization. Note that due to error retrying, some of these items may have appeared to 'load' earlier, but they have not.", dedupedExceptions.Length, dedupedExceptions.Length == 1 ? string.Empty : "s");

			sb.Append(string.Join("", dedupedExceptions.Select(x =>
			{
				var result = new StringBuilder();

				result.AppendFormat("<p style=\"margin-bottom: 0; font-size: 1.1em; color: orange;\">{0}</p>", x.Message);

				if (x.SerializedItemId != null)
				{
					result.AppendFormat("<p style=\"margin: 0; font-size: 0.7em; color: grey;\">{0}</p>", x.SerializedItemId);
				}

				Exception inner = x.InnerException;

				while (inner != null)
				{
					result.AppendFormat("<p style=\"margin-top: 0.2em; font-size: 1.1em;\" class=\"stacktrace\">&gt; {0}: {1}</p>", inner.GetType().Name, inner.Message);
					inner = inner.InnerException;
				}

				return result.ToString();
			})));
			sb.Append("<br />For full stack traces of each exception, see the Sitecore logs.");

			return sb.ToString();
		}

		private static void WriteInnerExceptionAsText(Exception innerException, StringBuilder exMessage)
		{
			if (innerException == null) return;

			exMessage.AppendLine();
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
