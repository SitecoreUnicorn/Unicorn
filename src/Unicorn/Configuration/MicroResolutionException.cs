using System;
using System.Runtime.Serialization;

namespace Unicorn.Configuration
{
	[Serializable]
	public class MicroResolutionException : Exception
	{
		public MicroResolutionException()
		{
		}

		public MicroResolutionException(string message) : base(message)
		{
		}

		public MicroResolutionException(string message, Exception inner) : base(message, inner)
		{
		}

		protected MicroResolutionException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
