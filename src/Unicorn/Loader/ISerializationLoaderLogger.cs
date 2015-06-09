using Rainbow.Model;

namespace Unicorn.Loader
{
	public interface ISerializationLoaderLogger
	{
		/// <summary>
		/// Called when loading of a tree of serialized items begins.
		/// </summary>
		void BeginLoadingTree(ISerializableItem rootSerializedItem);
		
		/// <summary>
		/// Called when loading of a tree of serialized items ends.
		/// </summary>
		void EndLoadingTree(ISerializableItem rootSerializedItem, int itemsProcessed, long elapsedMilliseconds);

		/// <summary>
		/// Called when an item is skipped by a predicate exclusion
		/// </summary>
		void SkippedItem(ISerializableItem skippedItem, string predicateName, string justification);
		
		/// <summary>
		/// This log method is called when an item is skipped because a predicate ignores it, HOWEVER it was also present in the serialization provider.
		/// For example, if "/foo/bar" was excluded, but the serialization provider returns an serialized item for /foo/bar, this will be called.
		/// This is not a fatal error, but usually implies that you have too many items in the provider.
		/// </summary>
		void SkippedItemPresentInSerializationProvider(ISerializableItem item, string predicateName, string serializationProviderName, string justification);
	}
}
