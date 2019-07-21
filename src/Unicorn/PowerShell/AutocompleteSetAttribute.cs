using System;

namespace Unicorn.PowerShell
{
	public class AutocompleteSetAttribute : Attribute
	{
		public string Values { get; private set; }

		public AutocompleteSetAttribute(string values)
		{
			Values = values;
		}
	}
}
