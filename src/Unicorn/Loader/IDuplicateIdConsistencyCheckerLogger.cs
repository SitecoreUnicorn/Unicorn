using Rainbow.Model;

namespace Unicorn.Loader
{
	public interface IDuplicateIdConsistencyCheckerLogger
	{
		void DuplicateFound(ISerializableItem existingItem, ISerializableItem duplicateItem);
	}
}
