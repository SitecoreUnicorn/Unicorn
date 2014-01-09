using System.Reflection;
using System.Web.UI;

namespace Unicorn.ControlPanel
{
	public class Heading : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h1>Unicorn Control Panel</h1>");
			var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			writer.Write("<p>Version {0} - <a href=\"https://github.com/kamsar/Unicorn\">Documentation</a> | <a href=\"https://github.com/kamsar/Unicorn/issues/new\">Report issue</a></p>", version);
		}
	}
}
