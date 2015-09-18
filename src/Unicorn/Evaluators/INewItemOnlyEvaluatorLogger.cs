using Rainbow.Model;

namespace Unicorn.Evaluators
{
	public interface INewItemOnlyEvaluatorLogger
	{
		/// <summary>
		/// Called when you serialize an item that does not yet exist in the provider
		/// </summary>
		void DeserializedNewItem(IItemData targetItem);

		/// <summary>
		/// Called when an item is evaluated regardless of the result (deleted, added, same, changed, etc)
		/// </summary>
		void Evaluated(IItemData item);
	}
}
