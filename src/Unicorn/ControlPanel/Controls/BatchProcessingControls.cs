using System.Web.UI;
using Unicorn.ControlPanel.Headings;

namespace Unicorn.ControlPanel.Controls
{
	internal class BatchProcessingControls : IControlPanelControl
	{
		private readonly bool _allowReserializeAll;

		// Sync all can make sense if all root paths exist but not all predicates resolve in Sitecore.
		// Reserialize (all), however, makes no sense if we have predicates that do not resolve. Those would just end up in exceptions.
		internal BatchProcessingControls(bool allowReserializeAll)
		{
			_allowReserializeAll = allowReserializeAll;
		}

		public void Render(HtmlTextWriter writer)
		{
			writer.Write($@"<h2 class=""syncall""><a data-basehref=""?verb=Sync"" href=""?verb=Sync"">{new HeadingService().GetAllTheThings()} Sync all the things!</a></h2>");

			string serializeAll = string.Empty;

			if (_allowReserializeAll)
			{
				serializeAll = @"
								<p>
									<a class=""button batch-reserialize"" href=""#"" onclick=""return confirm(&#39;DANGER: If any of these configurations use Transparent Sync, the items may not exist in the database and reserialize will reset to the database state! Continue?&#39;)"">Reserialize Selected</a>
								</p>";
			}

			writer.Write($@"
						<article class=""batch"">
							<section>
								<h4>Selected</h4>

								<ul class=""batch-configurations""></ul>
		
								<p>	
									<a class=""button batch-sync"" href=""#"">Sync Selected</a>
								</p>
								{serializeAll}
							</section>
						</article>");
		}
	}
}
