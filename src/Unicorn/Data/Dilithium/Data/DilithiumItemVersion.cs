using System.Collections.Generic;
using System.Globalization;
using Rainbow.Model;

namespace Unicorn.Data.Dilithium.Data
{
	public class DilithiumItemVersion : IItemVersion
	{
		public IEnumerable<IItemFieldValue> Fields => RawFields;
		public CultureInfo Language { get; set; }
		public int VersionNumber { get; set; }

		public IList<DilithiumFieldValue> RawFields { get; } = new List<DilithiumFieldValue>(); 
	}
}
