using Unicorn.Data;

namespace Unicorn.Serialization
{
	public interface ISerializationProvider
	{
		/// <summary>
		/// This moniker will be used when logging actions taken by this provider
		/// </summary>
		string LogName { get; }

		/// <summary>
		/// Serialize a given item into the serialization provider's serialization store
		/// </summary>
		ISerializedItem SerializeItem(ISourceItem item);

		/// <summary>
		/// Rename a serialized item in the provider. Note that the source item may NOT exist already in the provider.
		/// </summary>
		void RenameSerializedItem(ISourceItem renamedItem, string oldName);

		/// <summary>
		/// Move a serialized item in the provider. Note that the sourceItem may NOT exist already in the provider.
		/// </summary>
		/// <param name="sourceItem"></param>
		/// <param name="newParentItem"></param>
		void MoveSerializedItem(ISourceItem sourceItem, ISourceItem newParentItem);
		
		/// <summary>
		/// Get a serialized reference for a given Sitecore path and database. A reference is a pointer to the path,
		/// and while it indicates the store is capable of storing the path, it does not guarantee that path is serialized.
		/// </summary>
		ISerializedReference GetReference(ISourceItem sourceItem);
	}
}
