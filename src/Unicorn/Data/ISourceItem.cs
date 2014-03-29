using Sitecore.Data;

namespace Unicorn.Data
{
	/// <summary>
	/// A source item - e.g. a Sitecore item
	/// </summary>
	public interface ISourceItem
	{
		/// <summary>
		/// The name of the item in its tree
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The full path in the hierarchy of the item
		/// </summary>
		string ItemPath { get; }

		/// <summary>
		/// The name of the content database the item resides in
		/// </summary>
		string DatabaseName { get; }

		/// <summary>
		/// The unique ID of the item
		/// </summary>
		ID Id { get; }

		string TemplateName { get; }
		ID TemplateId { get; }

		/// <summary>
		/// The display to use for this item in status updates. Should allow for finding the item easily (e.g. "$database:$fullpath")
		/// </summary>
		string DisplayIdentifier { get; }

		/// <summary>
		/// Recycles or deletes the item
		/// </summary>
		void Recycle();

		FieldDictionary SharedFields { get; }
		ItemVersion[] Versions { get; }
		bool IsFieldComparable(string fieldId);

		/// <summary>
		/// Gets the item's child items
		/// </summary>
		ISourceItem[] Children { get; }
	}
}
