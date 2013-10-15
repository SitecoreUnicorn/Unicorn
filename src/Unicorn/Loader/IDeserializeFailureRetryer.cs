using System;
using System.Linq;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public interface IDeserializeFailureRetryer
	{
		void AddRetry(ISerializedReference reference, Exception exception);
		void RetryStandardValuesFailures(Action<ISerializedItem> retryAction);
		void RetryAll(ISourceDataProvider sourceDataProvider, Action<ISerializedItem> retrySingleItemAction, Action<ISerializedReference, IDeserializeFailureRetryer> retryTreeAction);
	}
}
