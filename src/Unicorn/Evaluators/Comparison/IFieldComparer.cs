using Rainbow.Model;

namespace Unicorn.Evaluators.Comparison
{
	public interface IFieldComparer
	{
		/// <remarks>The field passed may have a null type value.</remarks>
		bool CanCompare(ISerializableFieldValue field1, ISerializableFieldValue field2);

		bool AreEqual(ISerializableFieldValue field1, ISerializableFieldValue field2);
	}
}
