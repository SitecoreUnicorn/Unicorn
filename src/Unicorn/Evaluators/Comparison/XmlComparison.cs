using System;
using System.Xml.Linq;
using Rainbow.Model;

namespace Unicorn.Evaluators.Comparison
{
	public class XmlComparison : IFieldComparer
	{
		public bool CanCompare(ISerializableFieldValue field1, ISerializableFieldValue field2)
		{
			return (field1.FieldType != null && field1.FieldType.Equals("Layout", StringComparison.OrdinalIgnoreCase)) ||
			       (field2.FieldType != null && field2.FieldType.Equals("Layout", StringComparison.OrdinalIgnoreCase));
		}

		public bool AreEqual(ISerializableFieldValue field1, ISerializableFieldValue field2)
		{
			if (string.IsNullOrWhiteSpace(field1.Value) || string.IsNullOrWhiteSpace(field2.Value)) return false;

			var x1 = XElement.Parse(field1.Value);
			var x2 = XElement.Parse(field2.Value);

			return XNode.DeepEquals(x1, x2);
		}
	}
}
