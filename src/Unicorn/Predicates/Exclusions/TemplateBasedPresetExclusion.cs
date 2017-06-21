using System;
using Rainbow.Model;

namespace Unicorn.Predicates.Exclusions
{
	/// <summary>
	/// Excludes items with a given template ID
	/// e.g. <exclude templateId="{3B4F2B85-778D-44F3-9B2D-BEFF1F3575E6}" /> in config
	/// </summary>
	public class TemplateBasedPresetExclusion : IPresetTreeExclusion
	{
		private readonly string _excludedTemplate;

		public TemplateBasedPresetExclusion(string templateId)
		{
			_excludedTemplate = templateId;
		}

		public string Description => $"items with template id: {_excludedTemplate}";

		public PredicateResult Evaluate(IItemData itemData)
		{
			if (itemData.TemplateId.Equals(new Guid(_excludedTemplate)))
			{
				return new PredicateResult($"Item template id exclusion rule: {_excludedTemplate}");
			}
			return new PredicateResult(true);
		}
	}
}