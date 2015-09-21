using System;
using System.Text;

namespace Unicorn.Logging.Formatting
{
	public interface IExceptionFormatter
	{
		bool Format(Exception exception, StringBuilder result, bool asHtml);
	}
}
