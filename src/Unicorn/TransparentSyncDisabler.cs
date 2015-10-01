using Sitecore.Common;

namespace Unicorn
{
	/// <summary>
	/// Disables transparent sync on the current thread while it is in scope. Use with a using code block so it is always disposed.
	/// If transparent sync is disabled in config, does nothing. Other than...disable it further?
	/// </summary>
	public class TransparentSyncDisabler : Switcher<bool, TransparentSyncDisabler>
	{
		public TransparentSyncDisabler() : this(true)
		{
			
		}

		protected TransparentSyncDisabler(bool value) : base(value)
		{
			
		}
	}
}
