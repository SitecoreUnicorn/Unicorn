using System.Collections.Generic;
using System.Globalization;
using Rainbow.Model;

namespace Unicorn.Data.Dilithium.Sql
{
	public class SqlItemVersion : IItemVersion
	{
		public IEnumerable<IItemFieldValue> Fields => RawFields;
		public CultureInfo Language { get; set; }
		public int VersionNumber { get; set; }

		public IList<SqlItemFieldValue> RawFields { get; } = new List<SqlItemFieldValue>(); 
	}
}
