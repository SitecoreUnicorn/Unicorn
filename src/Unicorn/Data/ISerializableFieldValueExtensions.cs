using System;
using Gibson.Model;

namespace Unicorn.Data
{
	public static class SerializableFieldValueExtensions
	{
		public static bool IsFieldComparable(this ISerializableFieldValue field)
		{
			if (field == null) return false;

			return !field.FieldType.Equals("attachment", StringComparison.OrdinalIgnoreCase);
		}
	}
}
