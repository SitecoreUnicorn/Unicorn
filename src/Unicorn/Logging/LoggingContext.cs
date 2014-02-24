using System;
using Unicorn.Dependencies;

namespace Unicorn.Logging
{
	public sealed class LoggingContext : IDisposable
	{
		private readonly ILogger _logger;
		private readonly PubSubLogger _eventSource;

		public LoggingContext(ILogger logger, IConfiguration configuration)
		{
			_logger = logger;
			_eventSource = configuration.Resolve<ILogger>() as PubSubLogger;

			if(_eventSource == null) throw new InvalidOperationException("You can only use logger contexts with PubSubLogger registered as the ILogger dependency.");

			_eventSource.RegisterSubscriber(_logger);
		}

		public void Dispose()
		{
			_eventSource.DeregisterSubscriber(_logger);
		}
	}
}

/*
TODO
ported stuff to use ILogger
Need to make it simple to use
Add ILogger to default dep registry (pubsub logger)
Fix tests for ILogger
Need to implement multiple-configuration design - some classes' ctor params will need to allow for multiple, possibly
 * XML config grammar (default + extensions thereof)
*/