using Sitecore.Data;

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

		SerializedFieldDictionary SharedFields { get; }
		SerializedVersion[] Versions { get; }
		void RemoveVersion(string language, int versionNumber);
		void RemoveVersions(string language);
	}
}
