using System.Web.UI;

namespace Unicorn.ControlPanel
{
	public class Html5Footer : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write(" </body></html>");
		}
	}
}
