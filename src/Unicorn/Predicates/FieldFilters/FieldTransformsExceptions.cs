using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicorn.Predicates.FieldFilters
{
	public class MalformedFieldFilterException : Exception { public MalformedFieldFilterException(string message) : base(message) { } };
	public class DuplicateFieldsException : Exception { public DuplicateFieldsException(string message) : base(message) { } };
}
