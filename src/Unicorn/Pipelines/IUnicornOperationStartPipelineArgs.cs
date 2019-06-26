using Rainbow.Model;
using Unicorn.Configuration;
using Unicorn.Logging;

namespace Unicorn.Pipelines
{
	/// <summary>
	/// Exists to provide a common interface for args in processors that need to run both
	/// on sync start and on reserialize start.
	/// </summary>
	public interface IUnicornOperationStartPipelineArgs
	{
		OperationType Type { get; }
		IConfiguration[] Configurations { get; }
		ILogger Logger { get; }
		IItemData PartialOperationRoot { get; }
	}
}