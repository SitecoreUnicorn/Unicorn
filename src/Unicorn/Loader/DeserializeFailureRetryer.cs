using System;
using System.Collections.Generic;
using System.Linq;
using Kamsar.WebConsole;
using Sitecore.Diagnostics;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Loader
{
	public class DeserializeFailureRetryer : IDeserializeFailureRetryer
	{
		private readonly List<Failure> _failures = new List<Failure>();

		public void AddRetry(ISerializedReference reference, Exception exception)
		{
			Assert.ArgumentNotNull(reference, "reference");
			Assert.ArgumentNotNull(exception, "exception");

			_failures.Add(new Failure(reference, exception));
		}

		public void RetryStandardValuesFailures(IProgressStatus progress, Action<ISerializedItem, IProgressStatus> retryAction)
		{
			Assert.ArgumentNotNull(progress, "progress");
			Assert.ArgumentNotNull(retryAction, "retryAction");

			// find all failures caused by a StandardValuesException
			var standardValuesFailures = _failures.Where(x => x.Reason is StandardValuesException).ToArray();

			// remove those failures from the main list - we're about to retry them again
			_failures.RemoveAll(x => x.Reason is StandardValuesException);

			foreach (Failure failure in standardValuesFailures)
			{
				var item = failure.Reference as ISerializedItem;
				if (item != null)
				{
					try
					{
						retryAction(item, progress);
					}
					catch (Exception reason)
					{
						_failures.Add(new Failure(failure.Reference, reason));
					}
				}
			}
		}

		public void RetryAll(IProgressStatus progress, ISourceDataProvider sourceDataProvider, Action<ISerializedItem, IProgressStatus> retrySingleItemAction, Action<ISerializedReference, IDeserializeFailureRetryer, IProgressStatus> retryTreeAction)
		{
			Assert.ArgumentNotNull(progress, "progress");
			Assert.ArgumentNotNull(sourceDataProvider, "sourceDataProvider");
			Assert.ArgumentNotNull(retrySingleItemAction, "retrySingleItemAction");
			Assert.ArgumentNotNull(retryTreeAction, "retryTreeAction");

			if (_failures.Count > 0)
			{
				List<Failure> originalFailures;
				do
				{
					sourceDataProvider.ResetTemplateEngine();

					// save existing failures collection
					originalFailures = new List<Failure>(_failures);

					// clear the failures collection - we'll re-add any that fail again by evaluating originalFailures
					_failures.Clear();

					foreach (var failure in originalFailures)
					{
						// retry loading a single item failure
						var item = failure.Reference as ISerializedItem;
						if (item != null)
						{
							try
							{
								retrySingleItemAction(item, progress);
							}
							catch (Exception reason)
							{
								_failures.Add(new Failure(failure.Reference, reason));
							}

							continue;
						}

						// retry loading a reference failure (note the continues in the above ensure execution never arrives here for items)
						retryTreeAction(failure.Reference, this, progress);
					}
				}
				while (_failures.Count > 0 && _failures.Count < originalFailures.Count); // continue retrying until all possible failures have been fixed
			}

			if (_failures.Count > 0)
			{
				foreach (var failure in _failures)
				{
					progress.ReportStatus(string.Format("Failed to load {0} permanently because {1}", failure.Reference, failure.Reason), MessageType.Error);
				}

				throw new Exception("Some directories could not be loaded: " + _failures[0].Reference, _failures[0].Reason);
			}
		}

		/// <summary>
		/// Represents a single failure in a recursive serialization load operation
		/// </summary>
		private class Failure
		{
			public ISerializedReference Reference { get; private set; }
			public Exception Reason { get; private set; }

			public Failure(ISerializedReference reference, Exception reason)
			{
				Reference = reference;
				Reason = reason;
			}
		}
	}
}
