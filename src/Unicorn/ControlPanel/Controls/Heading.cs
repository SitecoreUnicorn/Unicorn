using System.Web.UI;
using Unicorn.ControlPanel.Headings;
using Unicorn.Data.Dilithium;

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

				if (ReactorContext.IsActive)
				{
					writer.Write(@"<p class=""warning"">Dilithium cache context is active. <br><br>
								Do not sync any Dilithium configurations until the cache has released from the other sync or reserialize operation. 
								If this remains visible and no operations are in progress this may indicate a bug in Unicorn; 
								report what you did right before this in an issue on GitHub and then restart your app pool to clear.</p>");
				}

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
								
								<p>
								<input type=""checkbox"" id=""skipTransparent"" value=""1"">
								<label for=""skipTransparent"">When syncing multiple configurations, skip configurations using Transparent Sync</label>
								<br>
								</p>

								<p class=""help"">Skipping transparent sync configurations can make your development synchronizations faster.</p>

								<p class=""help""><strong>Note:</strong> Changes are saved immediately.</p>

								<p><a class=""button"" onclick=""$('.overlay').trigger('hide'); return false;"">Close</a></p>
							</div>");
			}
		}
	}
}
