using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Diagnostics;
using Unicorn.Data;

namespace Unicorn.Loader
{
	/// <summary>
	/// The failure retryer manages the queue of deserialization failures for later retry.
	/// This occurs, for example, if during a load operation you try to load an instance of a template
	/// before the template itself. The instance would be added to the retryer, and get retried after
	/// everything else, where it would then succeed because its template had been created.
	/// </summary>
	public class DeserializeFailureRetryer : IDeserializeFailureRetryer
	{
		private readonly List<Failure> _itemFailures = new List<Failure>();
		private readonly List<Failure> _treeFailures = new List<Failure>();
		private readonly object _collectionLock = new object();

		public virtual void AddItemRetry(IItemData item, Exception exception)
		{
			Assert.ArgumentNotNull(item, "reference");
			Assert.ArgumentNotNull(exception, "exception");

			lock (_collectionLock)
			{
				_itemFailures.Add(CreateFailure(item, exception));
			}
		}

		public virtual void AddTreeRetry(IItemData item, Exception exception)
		{
			Assert.ArgumentNotNull(item, "reference");
			Assert.ArgumentNotNull(exception, "exception");

			lock (_collectionLock)
			{
				_treeFailures.Add(CreateFailure(item, exception));
			}
		}

		public virtual void RetryAll(ISourceDataStore sourceDataStore, Action<IItemData> retrySingleItemAction, Action<IItemData> retryTreeAction)
		{
			Assert.ArgumentNotNull(sourceDataStore, "sourceDataProvider");
			Assert.ArgumentNotNull(retrySingleItemAction, "retrySingleItemAction");
			Assert.ArgumentNotNull(retryTreeAction, "retryTreeAction");

			if (_itemFailures.Count > 0)
			{
				List<Failure> originalItemFailures;

				do
				{
					sourceDataStore.ResetTemplateEngine();

					// save existing failures collection
					originalItemFailures = new List<Failure>(_itemFailures);

					// clear the failures collection - we'll re-add any that fail again by evaluating originalFailures
					_itemFailures.Clear();

					foreach (var failure in originalItemFailures)
					{
						// retry loading a single item failure
						var item = failure.Item;
						if (item != null)
						{
							try
							{
								retrySingleItemAction(item);
							}
							catch (Exception reason)
							{
								_itemFailures.Add(CreateFailure(failure.Item, reason));
							}

							continue;
						}

						// retry loading a reference failure (note the continues in the above ensure execution never arrives here for items)
						retryTreeAction(failure.Item);
					}
				} while (_itemFailures.Count > 0 && _itemFailures.Count < originalItemFailures.Count);
					// continue retrying until all possible failures have been fixed
			}

			if(_treeFailures.Count > 0) {
				List<Failure> originalTreeFailures;

				do
				{
					sourceDataStore.ResetTemplateEngine();

					// save existing failures collection
					originalTreeFailures = new List<Failure>(_treeFailures);

					// clear the failures collection - we'll re-add any that fail again by evaluating originalFailures
					_treeFailures.Clear();

					foreach (var failure in originalTreeFailures)
					{
						try
						{
							// retry loading a tree failure
							retryTreeAction(failure.Item);
						}
						catch (Exception reason)
						{
							_treeFailures.Add(CreateFailure(failure.Item, reason));
						}
					}
				}
				while (_treeFailures.Count > 0 && _treeFailures.Count < originalTreeFailures.Count); // continue retrying until all possible failures have been fixed
			}

			if (_itemFailures.Count > 0 || _treeFailures.Count > 0)
			{
				if (_itemFailures.All(fail => !fail.IsHardFailure) && _treeFailures.All(fail => !fail.IsHardFailure))
				{
					throw new DeserializationSoftFailureAggregateException("Non-fatal warnings occurred during loading.")
					{
						InnerExceptions = _itemFailures.Select(fail => fail.Reason).Concat(_treeFailures.Select(fail => fail.Reason)).ToArray()
					};
				}

				var exceptions = new List<DeserializationException>();

				foreach (var failure in _itemFailures)
				{
					exceptions.Add(new DeserializationException(failure.Item.GetDisplayIdentifier(), failure.Item, failure.Reason));
				}

				foreach (var failure in _treeFailures)
				{
					exceptions.Add(new DeserializationException($"Tree failed to load: {failure.Item.GetDisplayIdentifier()} (the error may be on a child of this item, see the details below)", failure.Item, failure.Reason));
				}

				throw new DeserializationAggregateException("Some directories could not be loaded.") { InnerExceptions = exceptions.ToArray() };
			}
		}

		protected virtual Failure CreateFailure(IItemData item, Exception reason)
		{
			return new Failure(item, reason);
		}

		/// <summary>
		/// Represents a single failure in a recursive serialization load operation
		/// </summary>
		protected class Failure
		{
			public Failure(IItemData item, Exception reason)
			{
				Item = item;
				Reason = reason;
			}

			public IItemData Item { get; }
			public Exception Reason { get; }

			public virtual bool IsHardFailure => !(Reason is TemplateMissingFieldException);
		}
	}
}
