using System.Web.UI;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Allows you to kick off a reserialize or sync manually from the control panel
	/// </summary>
	public class ControlOptions : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h2>Unicorn Control Options</h2>");

			writer.Write("<h3>Synchronize</h3>");
			writer.Write("<p>Run a synchronization operation, which will sync serialized items with Sitecore.</p>");
			writer.Write("<a class=\"button\" href=\"?verb=Sync\">Sync Now</a>");

			writer.Write("<h3>Reserialize Configured Items</h3>");
			writer.Write("<p>This sets the serialization provider to match what is in Sitecore. This can be useful if changing path configurations or if you want to reset your serialized state.</p>");
			writer.Write("<a class=\"button\" href=\"?verb=Reserialize\" onclick=\"return confirm('This will reset the serialized state to match Sitecore. This normally is not needed after initial setup unless changing path configuration. Continue?')\">Reserialize Now</a>");
		}
	}
}
