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

		public void AddItemRetry(ISerializableItem reference, Exception exception)
		{
			Assert.ArgumentNotNull(reference, "reference");
			Assert.ArgumentNotNull(exception, "exception");

			_itemFailures.Add(new Failure(reference, exception));
		}

		public void AddTreeRetry(ISerializableItem reference, Exception exception)
		{
			Assert.ArgumentNotNull(reference, "reference");
			Assert.ArgumentNotNull(exception, "exception");

			_treeFailures.Add(new Failure(reference, exception));
		}

		public void RetryStandardValuesFailures(Action<ISerializableItem> retryAction)
		{
			Assert.ArgumentNotNull(retryAction, "retryAction");

			// find all failures caused by a StandardValuesException
			var standardValuesFailures = _itemFailures.Where(x => x.Reason is StandardValuesException).ToArray();

			// remove those failures from the main list - we're about to retry them again
			_itemFailures.RemoveAll(x => x.Reason is StandardValuesException);

			foreach (Failure failure in standardValuesFailures)
			{
				var item = failure.Reference as ISerializableItem;
				if (item != null)
				{
					try
					{
						retryAction(item);
					}
					catch (Exception reason)
					{
						_itemFailures.Add(new Failure(failure.Reference, reason));
					}
				}
			}
		}

		public void RetryAll(ISourceDataStore sourceDataProvider, Action<ISerializableItem> retrySingleItemAction, Action<ISerializableItem> retryTreeAction)
		{
			Assert.ArgumentNotNull(sourceDataProvider, "sourceDataProvider");
			Assert.ArgumentNotNull(retrySingleItemAction, "retrySingleItemAction");
			Assert.ArgumentNotNull(retryTreeAction, "retryTreeAction");

			if (_itemFailures.Count > 0)
			{
				List<Failure> originalItemFailures;
				List<Failure> originalTreeFailures;

				do
				{
					sourceDataProvider.ResetTemplateEngine();

					// save existing failures collection
					originalItemFailures = new List<Failure>(_itemFailures);

					// clear the failures collection - we'll re-add any that fail again by evaluating originalFailures
					_itemFailures.Clear();

					foreach (var failure in originalItemFailures)
					{
						// retry loading a single item failure
						var item = failure.Reference as ISerializableItem;
						if (item != null)
						{
							try
							{
								retrySingleItemAction(item);
							}
							catch (Exception reason)
							{
								_itemFailures.Add(new Failure(failure.Reference, reason));
							}

							continue;
						}

						// retry loading a reference failure (note the continues in the above ensure execution never arrives here for items)
						retryTreeAction(failure.Reference);
					}
				}
				while (_itemFailures.Count > 0 && _itemFailures.Count < originalItemFailures.Count); // continue retrying until all possible failures have been fixed

				do
				{
					sourceDataProvider.ResetTemplateEngine();

					// save existing failures collection
					originalTreeFailures = new List<Failure>(_treeFailures);

					// clear the failures collection - we'll re-add any that fail again by evaluating originalFailures
					_treeFailures.Clear();

					foreach (var failure in originalTreeFailures)
					{

						// retry loading a tree failure
						retryTreeAction(failure.Reference);
					}
				}
				while (_treeFailures.Count > 0 && _treeFailures.Count < originalTreeFailures.Count); // continue retrying until all possible failures have been fixed
			}

			if (_itemFailures.Count > 0 || _treeFailures.Count > 0)
			{
				var exceptions = new List<DeserializationException>();

				foreach (var failure in _itemFailures)
				{
					exceptions.Add(new DeserializationException(string.Format("Failed to load {0} permanently because {1}", failure.Reference.GetDisplayIdentifier(), failure.Reason), failure.Reason));
				}

				foreach (var failure in _treeFailures)
				{
					exceptions.Add(new DeserializationException(string.Format("Failed to load {0} (tree) permanently because {1}", failure.Reference.GetDisplayIdentifier(), failure.Reason), failure.Reason));
				}

				throw new DeserializationAggregateException("Some directories could not be loaded.") { InnerExceptions = exceptions.ToArray() };
			}
		}

		/// <summary>
		/// Represents a single failure in a recursive serialization load operation
		/// </summary>
		private class Failure
		{
			public ISerializableItem Reference { get; private set; }
			public Exception Reason { get; private set; }

			public Failure(ISerializableItem reference, Exception reason)
			{
				Reference = reference;
				Reason = reason;
			}
		}
	}
}
