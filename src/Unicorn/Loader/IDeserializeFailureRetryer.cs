using System;
using Rainbow.Model;
using Unicorn.Data;

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
		void AddItemRetry(ISerializableItem reference, Exception exception);
		void AddTreeRetry(ISerializableItem reference, Exception exception);
		void RetryStandardValuesFailures(Action<ISerializableItem> retryAction);
		void RetryAll(ISourceDataStore sourceDataProvider, Action<ISerializableItem> retrySingleItemAction, Action<ISerializableItem> retryTreeAction);
	}
}
