using System.Collections.Generic;

namespace Unicorn.Serialization
{
	/// <summary>
	/// A set of fields in a serialized item. NOTE: this dictionary is expected to be indexed *BY FIELD ID* not name.
	/// </summary>
	public class SerializedFieldDictionary : Dictionary<string, string>
	{
	}
}
