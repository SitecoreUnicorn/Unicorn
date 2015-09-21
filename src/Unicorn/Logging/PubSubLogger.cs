using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Unicorn.Logging
{
	/// <summary>
	/// This logger receives log entries and publishes them out to any number of loggers that are subscribed to it.
	/// This enables transiently adding and removing logging types at runtime.
	/// Note that by default this class automatically registers a SitecoreLogger as a subscriber, which makes everything
	/// get written to the Sitecore logs.
	/// 
	/// See also LoggingContext.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class PubSubLogger : ILogger
	{
		public PubSubLogger()
		{
			RegisterSubscriber(new SitecoreLogger());
		}

		private readonly Collection<ILogger> _loggers = new Collection<ILogger>();

		public void Info(string message)
		{
			foreach (var logger in _loggers)
			{
				logger.Info(message);
			}
		}

		public void Debug(string message)
		{
			foreach (var logger in _loggers)
			{
				logger.Debug(message);
			}
		}

		public void Warn(string message)
		{
			foreach (var logger in _loggers)
			{
				logger.Warn(message);
			}
		}

		public void Error(string message)
		{
			foreach (var logger in _loggers)
			{
				logger.Error(message);
			}
		}

		public void Error(Exception exception)
		{
			foreach (var logger in _loggers)
			{
				logger.Error(exception);
			}
		}

		public void RegisterSubscriber(ILogger logger)
		{
			_loggers.Add(logger);
		}

		public void DeregisterSubscriber(ILogger logger)
		{
			_loggers.Remove(logger);
		}
	}
}
