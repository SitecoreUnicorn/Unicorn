using System;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public interface IDeserializeFailureRetryer
	{
		void AddItemRetry(ISerializedReference reference, Exception exception);
		void AddTreeRetry(ISerializedReference reference, Exception exception);
		void RetryStandardValuesFailures(Action<ISerializedItem> retryAction);
		void RetryAll(ISourceDataProvider sourceDataProvider, Action<ISerializedItem> retrySingleItemAction, Action<ISerializedReference> retryTreeAction);
	}
}
