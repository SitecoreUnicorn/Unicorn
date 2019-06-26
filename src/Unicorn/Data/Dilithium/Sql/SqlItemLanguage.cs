using System.Collections.Generic;
using System.Globalization;
using Rainbow.Model;

namespace Unicorn.Data.Dilithium.Sql
{
	public class SqlItemLanguage : IItemLanguage
	{
		public IEnumerable<IItemFieldValue> Fields => RawFields;

		public CultureInfo Language { get; set; }

		public IList<SqlItemFieldValue> RawFields { get; } = new List<SqlItemFieldValue>(); 
	}
}
