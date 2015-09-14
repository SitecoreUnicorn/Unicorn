using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Unicorn
{
	[Serializable]
	public class DeserializationSoftFailureAggregateException : Exception
	{
		public DeserializationSoftFailureAggregateException()
		{
			InnerExceptions = new Exception[0];
		}

		public DeserializationSoftFailureAggregateException(string message) : base(message)
		{
			InnerExceptions = new Exception[0];
		}

		public DeserializationSoftFailureAggregateException(string message, Exception inner) : base(message, inner)
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
