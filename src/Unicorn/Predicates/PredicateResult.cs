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

		public bool IsIncluded { get; private set; }
		public string Justification { get; private set; }
	}
}
