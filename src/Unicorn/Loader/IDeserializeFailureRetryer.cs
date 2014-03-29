using System;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	/// <summary>
	/// The failure retryer manages the queue of deserialization failures for later retry.
	/// This occurs, for example, if during a load operation you try to load an instance of a template
	/// before the template itself. The instance would be added to the retryer, and get retried after
	/// everything else, where it would then succeed because its template had been created.
	/// </summary>
	public interface IDeserializeFailureRetryer
	{
		void AddItemRetry(ISerializedReference reference, Exception exception);
		void AddTreeRetry(ISerializedReference reference, Exception exception);
		void RetryStandardValuesFailures(Action<ISerializedItem> retryAction);
		void RetryAll(ISourceDataProvider sourceDataProvider, Action<ISerializedItem> retrySingleItemAction, Action<ISerializedReference> retryTreeAction);
	}
}
