using System.Web;
using System.Web.UI;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Allows you to kick off a reserialize or sync manually from the control panel
	/// </summary>
	public class ControlOptions : IControlPanelControl
	{
		public string ConfigurationName { get; set; }

		public void Render(HtmlTextWriter writer)
		{
			var configurationName = ConfigurationName ?? "All Configurations";
			var encodedConfigurationName = HttpUtility.UrlEncode(ConfigurationName ?? string.Empty);

			writer.Write("<h4>Synchronize</h4>");
			writer.Write("<p>Run a synchronization operation, which will sync serialized items with Sitecore.</p>");
			writer.Write("<a class=\"button\" href=\"?verb=Sync&amp;configuration={0}\">Sync <em>{1}</em> Now</a>", encodedConfigurationName, configurationName);

			writer.Write("<h4>Reserialize Configured Items</h4>");
			writer.Write("<p>This sets the serialization provider to match what is in Sitecore. This can be useful if changing path configurations or if you want to reset your serialized state.</p>");
			writer.Write("<a class=\"button\" href=\"?verb=Reserialize&amp;configuration={0}\" onclick=\"return confirm('This will reset the serialized state to match Sitecore. This normally is not needed after initial setup unless changing path configuration. Continue?')\">Reserialize <em>{1}</em> Now</a>", encodedConfigurationName, configurationName);
		}
	}
}
