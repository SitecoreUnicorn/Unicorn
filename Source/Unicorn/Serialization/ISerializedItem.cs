namespace Unicorn.Serialization
{
	public interface ISerializedItem
	{
		string Id { get; }
		string DatabaseName { get; }
		string ParentId { get; }
		string Name { get; }
		string BranchId { get; }
		string TemplateId { get; }
		string TemplateName { get; }
		string ItemPath { get; }
	}
}
