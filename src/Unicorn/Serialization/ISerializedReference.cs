namespace Unicorn.Serialization
{
	/// <summary>
	/// Represents a reference to a Sitecore path in the serialization provider. There may not be an actual item
	/// serialized for this path.
	/// 
	/// For example, in the Sitecore provider you can have a reference to a path folder, but that doesnt mean
	/// a .item exists for that path.
	/// </summary>
	public interface ISerializedReference
	{
		string ItemPath { get; }
		string DatabaseName { get; }

		/// <summary>
		/// The value to display in logs for this reference (e.g. might be an item ID/path, etc)
		/// </summary>
		string DisplayIdentifier { get; }

		/// <summary>
		/// An identifier unique to the serialization provider. Must exist.
		/// For example on a disk based system this might be the full path to the item.
		/// Should reflect something that would assist finding the serialized item in the storage.
		/// </summary>
		string ProviderId { get; }
	}
}
