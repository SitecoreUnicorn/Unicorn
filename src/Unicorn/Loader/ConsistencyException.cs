using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Unicorn.Loader
{
	/// <summary>
	/// Thrown when a load detects an inconsistency in the serialization store
	/// </summary>
	[Serializable, ExcludeFromCodeCoverage]
	public class ConsistencyException : Exception
	{
		public ConsistencyException()
		{
		}

		public ConsistencyException(string message) : base(message)
		{
		}

		public ConsistencyException(string message, Exception inner) : base(message, inner)
		{
		}

		protected ConsistencyException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
