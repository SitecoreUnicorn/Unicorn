using System;
using System.Text;
using Rainbow.Storage.Sc.Deserialization;

namespace Unicorn.Logging.Formatting
{
	public class TemplateMissingFieldExceptionFormatter : IExceptionFormatter
	{
		public bool Format(Exception exception, StringBuilder result, bool asHtml)
		{
			if (!asHtml) return false;

			var fieldMissingException = exception as TemplateMissingFieldException;
			if (fieldMissingException == null)
			{
				var nonFatalInner = exception.InnerException as TemplateMissingFieldException;
				if (nonFatalInner != null)
				{
					fieldMissingException = nonFatalInner;
				}
				else return false;
			}

			result.AppendFormat("<span class=\"warning line\">While loading {0}</span>", fieldMissingException.ItemIdentifier);
			result.AppendFormat("<span class=\"warning line-inner\">{0}<br>This usually occurs because a template field was deleted, and a serialized item using that template has a value for the deleted field.<br>You can resolve this warning by reserializing the {1} item, or manually removing the deleted field value from the serialized item.</span>", fieldMissingException.Message, fieldMissingException.ItemIdentifier);

			return true;
		}
	}
}
