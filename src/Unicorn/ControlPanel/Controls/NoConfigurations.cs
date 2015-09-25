using System.Web.UI;

namespace Unicorn.ControlPanel.Controls
{
	/// <summary>
	/// Shown when no configurations exist
	/// </summary>
	internal class NoConfigurations : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<article>");
			writer.Write("<h2>No Configurations Defined</h2>");

			writer.Write("<p>No Unicorn configurations have been defined. A configuration is a set of items to have Unicorn sync; in advanced usage can also customize how Unicorn works for that configuration.</p>");
			writer.Write("<p>To help with defining your first configuration, see the <code>Unicorn.Configs.Default.example</code> that should be in App_Config\\Include\\Unicorn for an example you can use as a starting point.</p>");
			writer.Write("<p>Duplicate the example file and change its extension to .config, then read through the comments in the file to assist you in setting up. When you're done, refresh this page and you can perform your initial serialization.</p>");
			writer.Write("</article>");
		}
	}
}
