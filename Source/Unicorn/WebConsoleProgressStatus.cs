using System;
using System.Globalization;
using System.Timers;
using Kamsar.WebConsole;

namespace Unicorn
{
	/// <summary>
	/// Implements a progress pattern using a WebConsole as the output source
	/// </summary>
	public class WebConsoleProgressStatus : IProgressStatus, IDisposable
	{
		readonly WebConsole _console;
		readonly int _subtaskIndex = 0;
		readonly int _subtaskCount = 100;
		Timer _heartbeat;
		DateTime _startTime;
		readonly string _taskName;

		public WebConsoleProgressStatus(string taskName, WebConsole console)
		{
			_console = console;
			_taskName = taskName;

			InitializeStatus();
		}

		public WebConsoleProgressStatus(string taskName, WebConsole console, int subtaskIndex, int subtaskCount)
			: this(taskName, console)
		{
			_subtaskIndex = subtaskIndex;
			_subtaskCount = subtaskCount;
		}

		public void Report(int percent)
		{
			SetProgress(percent);
		}

		public void Report(int percent, string statusMessage)
		{
			Report(percent, statusMessage, MessageType.Info);
		}

		public void Report(int percent, string statusMessage, MessageType type)
		{
			SetProgress(percent);
			_console.WriteLine(statusMessage, type);
		}

		public void ReportStatus(string statusMessage)
		{
			ReportStatus(statusMessage, MessageType.Info);
		}

		public void ReportStatus(string statusMessage, MessageType type)
		{
			_console.WriteLine(statusMessage, type);
		}

		public void ReportException(Exception exception)
		{
			_console.WriteException(exception);
		}

		public void Dispose()
		{
			SetProgress(100);
		}

		private void SetProgress(int taskProgress)
		{
			_console.SetTaskProgress(_subtaskIndex, _subtaskCount, taskProgress);
			if (taskProgress == 100)
			{
				_heartbeat.Stop();

				_console.SetProgressStatus("{0} has completed in  {1} sec", _taskName, Math.Round((DateTime.Now - _startTime).TotalSeconds).ToString(CultureInfo.InvariantCulture));

				_heartbeat.Dispose();
			}
		}

		private void InitializeStatus()
		{
			_console.SetProgressStatus(_taskName + " running");

			_startTime = DateTime.Now;
			_heartbeat = new Timer(2000);
			_heartbeat.AutoReset = true;
			_heartbeat.Elapsed += (sender, args) =>
			{
				var elapsed = Math.Round((args.SignalTime - _startTime).TotalSeconds);

				_console.SetProgressStatus("{0} running ({1} sec)", _taskName, elapsed.ToString(CultureInfo.InvariantCulture));
			};
			_heartbeat.Start();
		}
	}
}
