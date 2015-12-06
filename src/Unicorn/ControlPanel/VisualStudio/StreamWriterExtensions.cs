using System;
using System.IO;
using System.Text;
using Sitecore.StringExtensions;
using Unicorn.ControlPanel.VisualStudio.Logging;

namespace Unicorn.ControlPanel.VisualStudio
{
	internal static class StreamWriterExtensions
	{
		public static void SendMessage(this StreamWriter writer, ReportType type, MessageLevel level, string message)
		{
			var encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
			var report = "{0}|{1}|{2}".FormatWith(type, level, encodedMessage);
			writer.WriteLine(report);
			writer.Flush();
		}
	}
}
