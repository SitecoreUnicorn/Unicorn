using System.Web.UI;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Shown when no configurations exist
	/// </summary>
	public class NoConfigurations : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h4>Initial Setup</h4>");

			writer.Write("<p>No Unicorn configurations have been defined. A configuration is a set of items to have Unicorn sync; in advanced usage can also customize how Unicorn works for that configuration.</p>");
			writer.Write("<p>To help with defining your first configuration, see the <code>Unicorn.Configs.Default.example</code> that should be in App_Config\\Include\\Unicorn for an example you can use as a starting point.</p>");
			writer.Write("<p>Just duplicate the example file and change its extension to .config, then read through the comments in the file to assist you in setting up. When you're done just refresh this page and you can perform your initial serialization.</p>");
		}
	}
}
