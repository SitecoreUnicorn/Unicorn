using System.Linq;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public interface IConsistencyChecker
	{
		bool IsConsistent(ISerializedItem item);
		void AddProcessedItem(ISerializedItem item);
	}
}
