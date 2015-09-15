using Rainbow;
using Rainbow.Storage;

namespace Unicorn.Data
{
	/// <summary>
	/// Wrapper over IDataStore so that Unicorn can differentiate between source and target data stores by C# types
	/// </summary>
	public interface ITargetDataStore : IDataStore, IDocumentable
	{
	}
}
