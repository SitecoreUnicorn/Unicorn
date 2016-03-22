using System.Web.UI;
using Unicorn.ControlPanel.Headings;

namespace Unicorn.ControlPanel.Controls
{
	internal class Heading : IControlPanelControl
	{
		private readonly bool _isAuthenticated;

		public Heading(bool isAuthenticated)
		{
			_isAuthenticated = isAuthenticated;
		}

		public void Render(HtmlTextWriter writer)
		{
			writer.Write(new HeadingService().GetControlPanelHeadingHtml());

			if (_isAuthenticated)
			{
				writer.Write($"<p class=\"version\">Version {UnicornVersion.Current} | <a href=\"#\" data-modal=\"options\">Options</a></p>");

				writer.Write(@"<div class=""overlay"" id=""options"">
								<article class=""modal"">
								<label for=""verbosity"">Sync/reserialize console verbosity</label>
								<select id=""verbosity"">
									<option value=""Debug"">Items synced + detailed info</option>
									<option value=""Info"" selected>Items synced</option>
									<option value=""Warning"">Warnings and errors only</option>
									<option value=""Error"">Errors only</option>
								</select> 
								<br>
								<p class=""help"">Use lower verbosity when expecting many changes to avoid slowing down the browser.<br>Log files always get full verbosity.</p>
							</div>");
			}
		}
	}
}
