namespace Unicorn.Predicates
{
	public class PredicateResult
	{
		public PredicateResult(bool included)
		{
			IsIncluded = included;
		}

		public PredicateResult(string justification)
		{
			IsIncluded = false;
			Justification = justification;
		}

		public bool IsIncluded { get; }
		public string Justification { get; }
		
		/// <summary>
		/// Optional field which predicates may use to identify any sort of section within the predicate that the match came from.
		/// With the SerializationPresetPredicate, this is the include entry name that matched.
		/// </summary>
		public string PredicateComponentId { get; set; }
	}
}
