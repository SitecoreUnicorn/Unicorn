using System.Linq;
using System.Reflection;
using System.Web.UI;

namespace Unicorn.ControlPanel
{
	public class Heading : IControlPanelControl
	{
		public bool HasSerializedItems { get; set; }
		public bool IsAuthenticated { get; set; }

		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h1>Unicorn Control Panel</h1>");

			if (IsAuthenticated)
			{
				var version = (AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).Single();
				writer.Write("<small>Version {0} - <a href=\"https://github.com/kamsar/Unicorn\">Documentation</a> | <a href=\"https://github.com/kamsar/Unicorn/issues/new\">Report issue</a></small>", version.InformationalVersion);

				if (!HasSerializedItems)
				{
					writer.Write("<p class=\"warning\">Warning: you have not serialized any items yet. Unicorn cannot operate until this is complete. Please review the configuration below and then perform initial serialization if it is accurate. If you need to change your config, see App_Config\\Include\\Serialization.config.</p>");
				}
			}
		}
	}
}
