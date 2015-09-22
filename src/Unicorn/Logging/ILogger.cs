using System;

namespace Unicorn.Logging
{
	/// <summary>
	/// This is an abstraction to something that can receive log entries from Unicorn logger clases.
	/// </summary>
	public interface ILogger
	{
		void Info(string message);
		void Debug(string message);
		void Warn(string message);
		void Error(string message);
		void Error(Exception exception);
		void Flush();
	}
}
