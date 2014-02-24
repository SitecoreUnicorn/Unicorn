using System.Collections.ObjectModel;

namespace Unicorn.Logging
{
	public class PubSubLogger : ILogger
	{
		public PubSubLogger() : this(true)
		{
			
		}

		public PubSubLogger(bool registerSitecoreLogger)
		{
			if(registerSitecoreLogger)
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
