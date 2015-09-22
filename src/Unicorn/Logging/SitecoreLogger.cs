using System.Diagnostics.CodeAnalysis;
using Sitecore.Diagnostics;

namespace Unicorn.Logging
{
	/// <summary>
	/// Logger that writes to the Sitecore logs
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class SitecoreLogger : ILogger
	{
		public void Info(string message)
		{
			Log.Info("[Unicorn]: " + message, this);
		}

		public void Debug(string message)
		{
			// intentionally using Info() here so debug messages get written to logs with default settings
			Log.Info("[Unicorn]: " + message, this);
		}

		public void Warn(string message)
		{
			Log.Warn("[Unicorn]: " + message, this);
		}

		public void Error(string message)
		{
			Log.Error("[Unicorn]: " + message, this);
		}

		public void Error(System.Exception exception)
		{
			Log.Error(new ExceptionFormatter().FormatExceptionAsText(exception), this);
		}

		public void Flush()
		{
			
		}
	}
}
