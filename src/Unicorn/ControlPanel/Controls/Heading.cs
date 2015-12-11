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
					if (HasAllRootPaths)
						writer.Write("<p class=\"warning\">Warning: at least one configuration has not serialized any items yet. Unicorn cannot operate properly until this is complete. Please review the configuration below and then perform initial serialization if it is accurate.</p>");
					else 
						writer.Write("<p class=\"warning\">Warning: your current predicate configuration for at least one configuration does not have any valid root items defined. Nothing will be serialized until valid root items to start serializing from can be resolved. Please review your predicate configuration.</p>");
			}
		}
	}
}
