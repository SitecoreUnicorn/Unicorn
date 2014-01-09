using System;
using System.Collections.ObjectModel;

namespace Unicorn
{
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
