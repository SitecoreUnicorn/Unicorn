using System;

namespace Unicorn.Predicates.Fields
{
	public class MalformedFieldFilterException : Exception { public MalformedFieldFilterException(string message) : base(message) { } };
	public class DuplicateFieldsException : Exception { public DuplicateFieldsException(string message) : base(message) { } };
}
