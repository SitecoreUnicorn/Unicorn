using System;

namespace Rainbow.Predicates
{
	/// <summary>
	/// The Field Predicate is a way to exclude certain fields from being controlled by Unicorn.
	/// Note that the control is not complete in that the value of ignored fields is never stored;
	/// it is stored and updated when other fields' values that are included change.
	/// 
	/// However it is never deserialized or considered in the evaluator, and thus the value is effectively ignored.
	/// </summary>
	public interface IFieldPredicate
	{
		PredicateResult Includes(Guid fieldId);
		PredicateResult Includes(string fieldId);
	}
}
