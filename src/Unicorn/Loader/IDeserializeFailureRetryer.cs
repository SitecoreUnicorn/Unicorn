using System;
using System.Linq;
using Kamsar.WebConsole;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public interface IDeserializeFailureRetryer
	{
		void AddRetry(ISerializedReference reference, Exception exception);
		void RetryStandardValuesFailures(IProgressStatus progress, Action<ISerializedItem, IProgressStatus> retryAction);
		void RetryAll(IProgressStatus progress, ISourceDataProvider sourceDataProvider, Action<ISerializedItem, IProgressStatus> retrySingleItemAction, Action<ISerializedReference, IDeserializeFailureRetryer, IProgressStatus> retryTreeAction);
	}
}
