using System;
using Sitecore.Data.Events;
using Sitecore.SecurityModel;
using Unicorn.ControlPanel;

namespace Unicorn
{
	/// <summary>
	/// Disables Sitecore things as appripriate during a sync or reserialize
	/// </summary>
	public class UnicornOperationContext : IDisposable
	{
		private readonly SecurityDisabler _sd;
		private readonly ItemFilterDisabler _fd;
		private readonly EventDisabler _ed;
		private readonly TransparentSyncDisabler _tds;

		public UnicornOperationContext()
		{
			_sd = new SecurityDisabler();
			_ed = new EventDisabler(); // events, e.g. indexing, et al. This is what Sitecore's serialization API uses, and it's a superset of BulkUpdateContext
			_fd = new ItemFilterDisabler(); // disable all item filtering (if we're running in live mode we need this to get unadulterated items)
			_tds = new TransparentSyncDisabler();
		}

		public void Dispose()
		{
			_tds.Dispose();
			_fd.Dispose();
			_ed.Dispose();
			_sd.Dispose();
		}
	}
}
