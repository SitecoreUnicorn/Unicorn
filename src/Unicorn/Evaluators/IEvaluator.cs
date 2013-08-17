using Kamsar.WebConsole;
using Sitecore.Data.Items;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public interface IEvaluator
	{
		/// <summary>
		/// Orphans are items the loader has found that exist in Sitecore, but not in the serialization store.
		/// This method allows you to decide what to do with them (e.g. delete them, recycle them, serialize them elsewhere then delete, or do nothing)
		/// </summary>
		/// <param name="orphanItems"></param>
		/// <param name="progress"></param>
		void EvaluateOrphans(Item[] orphanItems, IProgressStatus progress);

		/// <summary>
		/// If a serialized item is found that has a corresponding item in Sitecore, this method is invoked to decide if it needs an update from serialized or not.
		/// Updating is pretty slow so it's much faster to skip unnecessary updates.
		/// </summary>
		/// <param name="serializedItem">The serialized item to evaluate</param>
		/// <param name="existingItem">The existing item in Sitecore</param>
		/// <returns>True to cause the serialized item to overwrite the existing item, false to leave the existing database item alone.</returns>
		bool EvaluateUpdate(ISerializedItem serializedItem, Item existingItem, IProgressStatus progress);
	}
}
