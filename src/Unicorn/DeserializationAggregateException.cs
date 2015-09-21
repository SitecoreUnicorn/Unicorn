using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Rainbow.Storage.Sc.Deserialization;

namespace Unicorn
{
	/// <summary>
	/// Thrown when a deserialization/load operation results in more than one fatal exception.
	/// </summary>
	[Serializable]
	[ExcludeFromCodeCoverage]
	public class DeserializationAggregateException : Exception
	{
		public DeserializationAggregateException(string message) : base(message)
		{
			InnerExceptions = new DeserializationException[0];
		}

		public DeserializationException[] InnerExceptions { get; set; }

		public override string Message
		{
			get { return base.Message + " (" + InnerExceptions.Length + " inner failures)\r\n" + string.Join("\r\n\r\n", InnerExceptions.Select(x => x.Message)); }
		}

		[ExcludeFromCodeCoverage]
		protected DeserializationAggregateException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
