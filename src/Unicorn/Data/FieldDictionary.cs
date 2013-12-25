using System.Collections.Generic;

namespace Unicorn.Data
{
	/// <summary>
	/// A set of fields in a serialized item. NOTE: this dictionary is expected to be indexed *BY FIELD ID* not name.
	/// </summary>
	public class FieldDictionary : Dictionary<string, string>
	{
	}
}
