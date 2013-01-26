using System;
using Kamsar.WebConsole;

namespace Unicorn
{
	/// <summary>
	/// Implementation of the progress pattern for Unicorn's use
	/// </summary>
	public interface IProgressStatus
	{
		void Report(int percent);
		void Report(int percent, string statusMessage);
		void Report(int percent, string statusMessage, MessageType type);
		void ReportException(Exception exception);
		void ReportStatus(string statusMessage);
		void ReportStatus(string statusMessage, MessageType type);
	}
}
