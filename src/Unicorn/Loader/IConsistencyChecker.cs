using Rainbow.Model;

namespace Unicorn.Loader
{
	/// <summary>
	/// The consistency checker allows you to hook into the load process to check for inconsistencies in the serialized data.
	/// For example the DuplicateIdConsistencyChecker keeps a list of processed IDs and if the same ID is processed twice throws an error.
	/// </summary>
	public interface IConsistencyChecker
	{
		bool IsConsistent(IItemData itemData);
		void AddProcessedItem(IItemData itemData);
	}
}
