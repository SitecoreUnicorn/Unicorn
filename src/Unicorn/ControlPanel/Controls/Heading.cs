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
				writer.Write("<p class=\"version\">Version {0}</p>", UnicornVersion.Current);
			}
		}
	}
}
