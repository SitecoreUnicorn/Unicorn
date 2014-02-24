using System;
using System.Collections.ObjectModel;

namespace Unicorn.Logging
{
	/// <summary>
	/// Allows 'storing up' log entries and writing them later.
	/// Used to show predicate messages after the deserialization of an item, rather than before it, which is confusing to read
	/// </summary>
	/// <typeparam name="TLog">Logger class type</typeparam>
	public class DeferredLogWriter<TLog>
	{
		private readonly Collection<Action<TLog>> _logEntries = new Collection<Action<TLog>>();

		public void AddEntry(Action<TLog> entryAction)
		{
			_logEntries.Add(entryAction);
		}

		public void ExecuteDeferredActions(TLog log)
		{
			foreach (var entry in _logEntries)
			{
				entry(log);
			}
		}
	}
}
