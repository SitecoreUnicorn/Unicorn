using System;
using Sitecore.Common;
using Sitecore.Diagnostics;

namespace Unicorn.Logging
{
	/// <summary>
	/// While this is in scope (with a using), log entries are queued up from this thread and written in a single batch on dispose
	/// Useful to maintain log order within groups of log entries during multithreading
	/// </summary>
	public class LogTransaction : Switcher<bool, LogTransaction>
	{
		private readonly ILogger _logger;

		public LogTransaction(ILogger logger) : base(true)
		{
			Assert.ArgumentNotNull(logger, "logger");

			_logger = logger;
			Disposed += OnDisposed;
		}

		private void OnDisposed(object sender, EventArgs e)
		{
			_logger.Flush();
		}
	}
}
