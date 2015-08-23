using Rainbow.Model;

namespace Unicorn.Loader
{
	public interface IDuplicateIdConsistencyCheckerLogger
	{
		void DuplicateFound(DuplicateIdConsistencyChecker.DuplicateIdEntry existingItemData, IItemData duplicateItemData);
	}
}
