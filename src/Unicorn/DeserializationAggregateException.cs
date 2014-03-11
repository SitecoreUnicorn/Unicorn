using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Unicorn
{
	[Serializable]
	public class DeserializationAggregateException : Exception
	{
		public DeserializationAggregateException()
		{
			InnerExceptions = new DeserializationException[0];
		}

		public DeserializationAggregateException(string message) : base(message)
		{
			InnerExceptions = new DeserializationException[0];
		}

		public DeserializationAggregateException(string message, Exception inner) : base(message, inner)
		{
			InnerExceptions = new DeserializationException[0];
		}

		public DeserializationException[] InnerExceptions { get; set; }

		public override string Message
		{
			get { return base.Message + " (" + InnerExceptions.Length + " inner failures)\r\n" + string.Join("\r\n\r\n", InnerExceptions.Select(x => x.Message)); }
		}

		protected DeserializationAggregateException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
