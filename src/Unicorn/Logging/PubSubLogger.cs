using System;
using System.Collections.Generic;
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
		// because this is thread static each processing thread maintains its own queue
		// combined with how LogTransaction's switcher is thread specific, this gives us transactions
		// per-thread
		[ThreadStatic] private static Queue<Action<ILogger>> _queue;
		private static readonly object WriteLock = new object();

		private readonly Collection<ILogger> _loggers = new Collection<ILogger>();

		public PubSubLogger()
		{
			// HEY YOU
			// we need a way to put something in scope to cause deferral of all writes to the log for the current thread.
			// this way we can log atomically in a multithreaded sync
			RegisterSubscriber(new SitecoreLogger());
		}

		public void Info(string message)
		{
			Queue(log => log.Info(message));
		}

		public void Debug(string message)
		{
			Queue(log => log.Debug(message));
		}

		public void Warn(string message)
		{
			Queue(log => log.Warn(message));
		}

		public void Error(string message)
		{
			Queue(log => log.Error(message));
		}

		public void Error(Exception exception)
		{
			Queue(log => log.Error(exception));
		}

		public void RegisterSubscriber(ILogger logger)
		{
			_loggers.Add(logger);
		}

		public void DeregisterSubscriber(ILogger logger)
		{
			_loggers.Remove(logger);
		}

		public void Flush()
		{
			if (_queue.Count == 0) return;
			if (LogTransaction.CurrentValue) return;

			lock (WriteLock)
			{
				if (LogTransaction.CurrentValue) return;

				while (_queue.Count > 0)
				{
					var entry = _queue.Dequeue();
					foreach (var logger in _loggers)
					{
						entry(logger);
					}
				}
			}
		}

		private void Queue(Action<ILogger> logEntry)
		{
			if(_queue == null) _queue = new Queue<Action<ILogger>>();
			_queue.Enqueue(logEntry);

			Flush();
		}
	}
}
