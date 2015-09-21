using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Unicorn.Logging.Formatting;

namespace Unicorn.Logging
{
	/// <summary>
	/// Formats exceptions all pretty-like so we can log them and show them in the HTML console intelligibly
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class ExceptionFormatter
	{
		protected List<IExceptionFormatter> Formatters = new List<IExceptionFormatter>();

		public ExceptionFormatter()
		{
			Formatters.Add(new TemplateMissingFieldExceptionFormatter());
			Formatters.Add(new DeserializationExceptionFormatter());
		}

		public virtual string FormatExceptionAsHtml(Exception exception)
		{
			var aggregateException = exception as DeserializationAggregateException;
			if (aggregateException != null) return FormatAggregateExceptionAsHtml(aggregateException);

			var softFailException = exception as DeserializationSoftFailureAggregateException;
			if (softFailException != null) return FormatSoftFailAsHtml(softFailException);

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

		public virtual string FormatExceptionAsText(Exception exception)
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

		protected virtual string FormatAggregateExceptionAsHtml(DeserializationAggregateException exception)
		{
			var sb = new StringBuilder();

			var dedupedExceptions = exception.InnerExceptions
				.GroupBy(ex => ex.Message)
				.Select(group => group.First())
				.ToArray();

			sb.AppendFormat("{0} error{1} occurred during deserialization. Earlier log messages for items below should be ignored; an error occurred that was retried and failed to correct.", dedupedExceptions.Length, dedupedExceptions.Length == 1 ? string.Empty : "s");

			foreach (var inner in dedupedExceptions)
			{
				FormatException(inner, sb, true);
			}

			sb.Append("<br />For full stack traces of each exception, see the Sitecore logs.");

			return sb.ToString();
		}

		protected virtual string FormatSoftFailAsHtml(DeserializationSoftFailureAggregateException exception)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("<span class=\"line warning\">{0} non-fatal warning{1} occurred during deserialization.</span>", exception.InnerExceptions.Length, exception.InnerExceptions.Length == 1 ? string.Empty : "s");

			foreach (var inner in exception.InnerExceptions)
			{
				FormatException(inner, sb, true);
			}

			return sb.ToString();
		}

		protected virtual void FormatException(Exception exception, StringBuilder result, bool asHtml)
		{
			foreach (var handler in Formatters)
			{
				if (handler.Format(exception, result, asHtml)) return;
			}
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
