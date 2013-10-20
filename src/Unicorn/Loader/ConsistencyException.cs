using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Unicorn.Loader
{
	[Serializable]
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
