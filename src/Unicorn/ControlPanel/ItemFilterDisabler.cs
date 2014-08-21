using Sitecore.Common;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Creates a disabler (IDisposable) for disabling item filtering as needed
	/// This is related to #26 (https://github.com/kamsar/Unicorn/issues/26) when running in live mode and syncing core db items.
	/// </summary>
	public class ItemFilterDisabler : Switcher<ItemFilterDisabler.FilterState>
	{
		public ItemFilterDisabler() : base(FilterState.Disabled)
		{
		}

		public enum FilterState { Enabled, Disabled }
	}
}
