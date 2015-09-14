using System;
using System.Text;
using Rainbow.Storage.Sc.Deserialization;

namespace Unicorn.Logging.Formatting
{
	public class DeserializationExceptionFormatter : IExceptionFormatter
	{
		public bool Format(Exception exception, StringBuilder result, bool asHtml)
		{
			if (!asHtml) return false;

			var fatalException = exception as DeserializationException;

			if (fatalException == null) return false;
			
			result.AppendFormat("<span class=\"line\">{0}</span>", exception.Message);

			if (fatalException.SerializedItemId != null)
			{
				result.AppendFormat("<span class=\"line-inner line-smaller\">{0}</span>", fatalException.SerializedItemId);
			}

			Exception inner = exception.InnerException;

			while (inner != null)
			{
				result.AppendFormat("<span class=\"line-inner\">&gt; {0}: {1}</span>", inner.GetType().Name, inner.Message);
				inner = inner.InnerException;
			}

			return true;
		}
	}
}
