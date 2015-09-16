using System;
using System.Diagnostics.CodeAnalysis;
using Unicorn.Configuration;

namespace Unicorn.Logging
{
	/// <summary>
	/// The LoggingContext allows you to temporarily register a new logger with the PubSubLogger for a finite period of execution.
	/// </summary>
	/// <example>
	///		using(new LoggingContext(new WebConsoleLogger(console))) {
	///			// code that should be logged to the web console too
	///		} // when using goes out of scope the logger is detatched from the PubSubLogger
	/// </example>
	[ExcludeFromCodeCoverage]
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