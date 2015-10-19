using System;
using Kamsar.WebConsole;

namespace Unicorn.ControlPanel.VisualStudio.Logging
{
	internal class NullProgressStatus : IProgressStatus
	{
		private int _percent;

		public void Report(int percent)
		{
			_percent = percent;
		}

		public void ReportException(Exception exception)
		{
			
		}

		public void ReportStatus(string statusMessage, params object[] formatParameters)
		{
			
		}

		public void ReportStatus(string statusMessage, MessageType type, params object[] formatParameters)
		{
			
		}

		public void ReportTransientStatus(string statusMessage, params object[] formatParameters)
		{
			
		}

		public int Progress { get { return _percent; } }
	}
}
