using Sitecore.Data;
using Unicorn.Data;

namespace Unicorn.Serialization
{
	public interface ISerializedItem : ISerializedReference
	{
		ID Id { get; }
		ID ParentId { get; }
		string Name { get; }
		ID BranchId { get; }
		ID TemplateId { get; }
		string TemplateName { get; }

		FieldDictionary SharedFields { get; }
		ItemVersion[] Versions { get; }

		/// <summary>
		/// Deserialize this item to Sitecore database
		/// </summary>
		/// <remarks>
		/// Should throw an exception if deserialization fails, probably a DeserializationException
		/// </remarks>
		ISourceItem Deserialize(bool ignoreMissingTemplateFields);

		/// <summary>
		/// Deletes an item from the serialization provider.
		/// </summary>
		void Delete();

		/// <summary>
		/// Checks if a serialized item is part of a template standard values
		/// </summary>
		/// <returns>True if the item is a standard values item, or false if it's not</returns>
		bool IsStandardValuesItem { get; }
	}
}
