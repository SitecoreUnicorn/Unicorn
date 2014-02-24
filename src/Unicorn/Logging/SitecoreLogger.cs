using Sitecore.Diagnostics;

namespace Unicorn.Logging
{
	public class SitecoreLogger : ILogger
	{
		public void Info(string message)
		{
			Log.Info(message, this);
		}

		public void Debug(string message)
		{
			// intentionally using Info() here so debug messages get written to logs with default settings
			Log.Info(message, this);
		}

		public void Warn(string message)
		{
			Log.Warn(message, this);
		}

		public void Error(string message)
		{
			Log.Error(message, this);
		}
	}
}
