using System;

namespace Unicorn.Predicates
{
	public class ExceptionRule
	{
		public string Name { get; set; }
		public bool IncludeChildren { get; set; }
		public string TemplateId { get; set; }
	}
}