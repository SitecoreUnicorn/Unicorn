using System;
using Kamsar.WebConsole;
using Sitecore.Diagnostics;

namespace Unicorn
{
	internal class SitecoreLogProgressStatus : IProgressStatus
	{
		private int _progress;

		public int Progress
		{
			get { return _progress; }
		}

		public void Report(int percent)
		{
			_progress = percent;
		}

		public void ReportException(Exception exception)
		{
			Log.Error("Unicorn exception!", exception, this);
		}

		public void ReportStatus(string statusMessage, MessageType type, params object[] formatParameters)
		{
			var message = "Unicorn: " + (formatParameters.Length > 0 ? string.Format(statusMessage, formatParameters) : statusMessage);

			switch (type)
			{
				case MessageType.Info: 
					Log.Info(message, this);
					return;
				case MessageType.Warning: 
					Log.Warn(message, this);
					return;
				case MessageType.Error: 
					Log.Error(message, this);
					return;
				case MessageType.Debug: 
					Log.Debug(message, this);
					return;
			}
		}

		public void ReportStatus(string statusMessage, params object[] formatParameters)
		{
			ReportStatus(statusMessage, MessageType.Info, formatParameters);
		}

		public void ReportTransientStatus(string statusMessage, params object[] formatParameters)
		{
			// do nothing, we don't log transients
		}
	}
}
