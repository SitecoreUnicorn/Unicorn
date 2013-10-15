using System;
using System.Linq;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Evaluators
{
	public interface ISerializedAsMasterEvaluatorLogger
	{
		/// <summary>
		/// Called when an item is evaluated for deletion
		/// </summary>
		void DeletedItem(ISourceItem deletedItem);

		/// <summary>
		/// Called when an item cannot be evaluated for an update (e.g. it had no valid criteria to compare)
		/// </summary>
		void CannotEvaluateUpdate(ISerializedItem serializedItem, SerializedVersion version);

		void IsModifiedMatch(ISerializedItem serializedItem, SerializedVersion version, DateTime serializedModified, DateTime itemModified);

		void IsRevisionMatch(ISerializedItem serializedItem, SerializedVersion version, string serializedRevision, string itemRevision);

		void IsNameMatch(ISerializedItem serializedItem, ISourceItem existingItem, SerializedVersion version);
	}
}
