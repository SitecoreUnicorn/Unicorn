using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace Unicorn
{
	/// <summary>
	/// Thrown when a deserialization/load operation results in more than one non-fatal warning.
	/// </summary>
	[Serializable]
	[ExcludeFromCodeCoverage]
	public class DeserializationSoftFailureAggregateException : Exception
	{
		public DeserializationSoftFailureAggregateException(string message) : base(message)
		{
			InnerExceptions = new Exception[0];
		}

		public Exception[] InnerExceptions { get; set; }

		public override string Message
		{
			get { return base.Message + " (" + InnerExceptions.Length + " inner failures)\r\n" + string.Join("\r\n\r\n", InnerExceptions.Select(x => x.Message)); }
		}

		protected DeserializationSoftFailureAggregateException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
