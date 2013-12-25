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
	}
}
