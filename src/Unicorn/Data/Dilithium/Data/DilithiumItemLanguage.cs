using System.Collections.Generic;
using System.Globalization;
using Rainbow.Model;

namespace Unicorn.Data.Dilithium.Data
{
	public class DilithiumItemLanguage : IItemLanguage
	{
		public IEnumerable<IItemFieldValue> Fields => RawFields;

		public CultureInfo Language { get; set; }

		public IList<DilithiumFieldValue> RawFields { get; } = new List<DilithiumFieldValue>(); 
	}
}
