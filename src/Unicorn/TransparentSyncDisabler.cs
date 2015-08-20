using Sitecore.Common;

namespace Unicorn
{
	public class TransparentSyncDisabler : Switcher<bool, TransparentSyncDisabler>
	{
		public TransparentSyncDisabler() : base(true)
		{
			
		}
	}
}
