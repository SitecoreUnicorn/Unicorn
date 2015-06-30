using Rainbow.Model;

namespace Unicorn.Evaluators.Comparison
{
	public class DefaultComparison : IFieldComparer
	{
		public bool CanCompare(ISerializableFieldValue field1, ISerializableFieldValue field2)
		{
			return field1 != null && field2 != null;
		}

		public bool AreEqual(ISerializableFieldValue field1, ISerializableFieldValue field2)
		{
			return field1.Value.Equals(field2.Value);
		}
	}
}
