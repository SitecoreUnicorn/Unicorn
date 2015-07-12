using Rainbow.Model;

namespace Unicorn.Loader
{
	public interface IDuplicateIdConsistencyCheckerLogger
	{
		void DuplicateFound(IItemData existingItemData, IItemData duplicateItemData);
	}
}
