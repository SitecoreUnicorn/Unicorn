using System;
using Sitecore.Diagnostics;

namespace Unicorn
{
	public  class StandardValuesException : Exception
	{
		public StandardValuesException(string itemPath)
			: base(itemPath)
		{
			Assert.ArgumentNotNull(itemPath, "itemPath");
		}

		public override string ToString()
		{
			return "Reverting of Standard values of template is delayed. " + Message;
		}
	}
}
