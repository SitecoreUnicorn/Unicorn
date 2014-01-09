using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public interface IEvaluator
	{
		/// <summary>
		/// Orphans are items the loader has found that exist in the source data, but not in the serialization store.
		/// This method allows you to decide what to do with them (e.g. delete them, recycle them, serialize them elsewhere then delete, or do nothing)
		/// </summary>
		/// <param name="orphanItems"></param>
		void EvaluateOrphans(ISourceItem[] orphanItems);

		/// <summary>
		/// If a serialized item is found that does not have a corresponding item in source data, this method is invoked to decide what to do about it.
		/// Normally, this would probably trigger the deserialization of the serialized item into source data.
		/// </summary>
		/// <param name="newItem">The new serialized item not present in source data</param>
		/// <returns>If a new source item is created, return it. If not, return null.</returns>
		ISourceItem EvaluateNewSerializedItem(ISerializedItem newItem);

		/// <summary>
		/// If a serialized item is found that has a corresponding item in source data, this method is invoked to perform any updates that are needed to the source data.
		/// Updating is pretty slow so it's much faster to skip unnecessary updates. Normally this would probably compare timestamps etc and if changed, trigger a deserialization.
		/// </summary>
		/// <param name="serializedItem">The serialized item to evaluate</param>
		/// <param name="existingItem">The existing item in Sitecore</param>
		/// <returns>If an update is performed, return the updated source item. If no update occurs, return null.</returns>
		ISourceItem EvaluateUpdate(ISerializedItem serializedItem, ISourceItem existingItem);
	}
}
