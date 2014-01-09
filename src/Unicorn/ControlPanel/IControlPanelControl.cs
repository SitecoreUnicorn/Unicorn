using System.Web.UI;

namespace Unicorn.ControlPanel
{
	interface IControlPanelControl
	{
		void Render(HtmlTextWriter writer);
	}
}
