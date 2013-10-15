using System.Linq;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public interface ISerializationLoaderLogger
	{
		/// <summary>
		/// Called when loading of a tree of serialized items begins.
		/// </summary>
		void BeginLoadingTree(ISerializedReference rootSerializedItem);
		
		/// <summary>
		/// Called when loading of a tree of serialized items ends.
		/// </summary>
		void EndLoadingTree(ISerializedReference rootSerializedItem, int itemsProcessed, long elapsedMilliseconds);

		/// <summary>
		/// Called when an item is skipped by a predicate exclusion
		/// </summary>
		void SkippedItem(ISourceItem skippedItem, string predicateName, string justification);
		
		/// <summary>
		/// This log method is called when an item is skipped because a predicate ignores it, HOWEVER it was also present in the serialization provider.
		/// For example, if "/foo/bar" was excluded, but the serialization provider returns an serialized item for /foo/bar, this will be called.
		/// This is not a fatal error, but usually implies that you have too many items in the provider.
		/// </summary>
		void SkippedItemPresentInSerializationProvider(ISerializedReference item, string predicateName, string serializationProviderName, string justification);

		/// <summary>
		/// This log method is called when an item is skipped because it did not exist in the serialization provider.
		/// For example, in Sitecore serialization terms, this would occur if you had:
		/// c:\master\templates\foo (exists)
		/// c:\master\templates\foo.item (does not exist)
		/// </summary>
		void SkippedItemMissingInSerializationProvider(ISerializedReference item, string serializationProviderName);

		/// <summary>
		/// Called when you serialize an item that does not yet exist in the provider
		/// </summary>
		void SerializedNewItem(ISerializedItem serializedItem);

		/// <summary>
		/// Called when you serialize an updated item that already existed in the provider
		/// </summary>
		void SerializedUpdatedItem(ISerializedItem serializedItem);
	}
}
