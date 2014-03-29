using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public interface IDuplicateIdConsistencyCheckerLogger
	{
		void DuplicateFound(ISerializedItem existingItem, ISerializedItem duplicateItem);
	}
}
