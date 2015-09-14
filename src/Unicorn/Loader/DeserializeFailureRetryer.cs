using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Diagnostics;
using Unicorn.Data;

namespace Unicorn.Loader
{
	public class DeserializeFailureRetryer : IDeserializeFailureRetryer
	{
		private readonly List<Failure> _itemFailures = new List<Failure>();
		private readonly List<Failure> _treeFailures = new List<Failure>();
		private readonly object _collectionLock = new object();

		public void AddItemRetry(IItemData item, Exception exception)
		{
			Assert.ArgumentNotNull(item, "reference");
			Assert.ArgumentNotNull(exception, "exception");

			lock (_collectionLock)
			{
				_itemFailures.Add(new Failure(item, exception));
			}
		}

		public void AddTreeRetry(IItemData item, Exception exception)
		{
			Assert.ArgumentNotNull(item, "reference");
			Assert.ArgumentNotNull(exception, "exception");

			lock (_collectionLock)
			{
				_treeFailures.Add(new Failure(item, exception));
			}
		}

		public void RetryStandardValuesFailures(Action<IItemData> retryAction)
		{
			Assert.ArgumentNotNull(retryAction, "retryAction");

			Failure[] standardValuesFailures;

			lock (_collectionLock)
			{
				// find all failures caused by a StandardValuesException
				standardValuesFailures = _itemFailures.Where(x => x.Reason is StandardValuesException).ToArray();

				// remove those failures from the main list - we're about to retry them again
				_itemFailures.RemoveAll(x => x.Reason is StandardValuesException);
			}

			foreach (Failure failure in standardValuesFailures)
			{
				var item = failure.Item;
				if (item != null)
				{
					try
					{
						retryAction(item);
					}
					catch (Exception reason)
					{
						lock (_collectionLock)
						{
							_itemFailures.Add(new Failure(failure.Item, reason));
						}
					}
				}
			}
		}

		public void RetryAll(ISourceDataStore sourceDataStore, Action<IItemData> retrySingleItemAction, Action<IItemData> retryTreeAction)
		{
			Assert.ArgumentNotNull(sourceDataStore, "sourceDataProvider");
			Assert.ArgumentNotNull(retrySingleItemAction, "retrySingleItemAction");
			Assert.ArgumentNotNull(retryTreeAction, "retryTreeAction");

			if (_itemFailures.Count > 0)
			{
				List<Failure> originalItemFailures;
				List<Failure> originalTreeFailures;

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
								_itemFailures.Add(new Failure(failure.Item, reason));
							}

							continue;
						}

						// retry loading a reference failure (note the continues in the above ensure execution never arrives here for items)
						retryTreeAction(failure.Item);
					}
				}
				while (_itemFailures.Count > 0 && _itemFailures.Count < originalItemFailures.Count); // continue retrying until all possible failures have been fixed

				do
				{
					sourceDataStore.ResetTemplateEngine();

					// save existing failures collection
					originalTreeFailures = new List<Failure>(_treeFailures);

					// clear the failures collection - we'll re-add any that fail again by evaluating originalFailures
					_treeFailures.Clear();

					foreach (var failure in originalTreeFailures)
					{
						// retry loading a tree failure
						retryTreeAction(failure.Item);
					}
				}
				while (_treeFailures.Count > 0 && _treeFailures.Count < originalTreeFailures.Count); // continue retrying until all possible failures have been fixed
			}

			if (_itemFailures.Count > 0 || _treeFailures.Count > 0)
			{
				var exceptions = new List<DeserializationException>();

				foreach (var failure in _itemFailures)
				{
					exceptions.Add(new DeserializationException(failure.Item.GetDisplayIdentifier(), failure.Item, failure.Reason));
				}

				foreach (var failure in _treeFailures)
				{
					exceptions.Add(new DeserializationException(string.Format("This tree failed to load: {0} (the error may not be on this item, see the details below)", failure.Item.GetDisplayIdentifier()), failure.Item, failure.Reason));
				}

				throw new DeserializationAggregateException("Some directories could not be loaded.") { InnerExceptions = exceptions.ToArray() };
			}
		}

		/// <summary>
		/// Represents a single failure in a recursive serialization load operation
		/// </summary>
		private class Failure
		{
			public IItemData Item { get; private set; }
			public Exception Reason { get; private set; }

			public Failure(IItemData item, Exception reason)
			{
				Item = item;
				Reason = reason;
			}
		}
	}
}
