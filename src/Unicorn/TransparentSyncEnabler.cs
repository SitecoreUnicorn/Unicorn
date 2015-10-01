namespace Unicorn
{
	/// <summary>
	/// Enables transparent sync on the current thread while it is in scope, if it was previously disabled. 
	/// Use with a using code block so it is always disposed.
	/// If transparent sync is disabled in config, does nothing. If TpSync is not disabler'ed, does nothing.
	/// </summary>
	public class TransparentSyncEnabler : TransparentSyncDisabler
	{
		public TransparentSyncEnabler() : base(false)
		{
			
		}
	}
}
