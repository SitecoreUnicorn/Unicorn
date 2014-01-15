using System.Web.UI;

namespace Unicorn.ControlPanel
{
	public class InitialSetup : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h2>Initial Setup</h2>");
			writer.Write("<p>Would you like to perform an initial serialization of all configured items using the options outlined above now? This is required to start using Unicorn.</p>");

			writer.Write("<a class=\"button\" href=\"?verb=Reserialize\">Perform Initial Serialization</a>");
		}
	}
}
