using System;
using Rainbow.Model;

namespace Unicorn.Data
{
	public static class SerializableFieldValueExtensions
	{
		public static bool IsFieldComparable(this IItemFieldValue field)
		{
			if (field == null) return false;

			if (field.FieldType == null) return true; // null = "unprocessed"

			return !field.FieldType.Equals("attachment", StringComparison.OrdinalIgnoreCase);
		}
	}
}
