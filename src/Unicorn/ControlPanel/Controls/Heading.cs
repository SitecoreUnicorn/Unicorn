using System.Linq;
using System.Reflection;
using System.Web.UI;
using Unicorn.ControlPanel.Headings;

namespace Unicorn.ControlPanel.Controls
{
	internal class Heading : IControlPanelControl
	{
		public bool HasSerializedItems { get; set; }
		public bool HasValidSerializedItems { get; set; }
		public bool IsAuthenticated { get; set; }

		public void Render(HtmlTextWriter writer)
		{
			writer.Write(new HeadingService().GetControlPanelHeadingHtml());

			if (IsAuthenticated)
			{
				var version = (AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).Single();
				writer.Write("<p class=\"version\">Version {0}</p>", version.InformationalVersion);

				if (!HasSerializedItems)
				{
					if(HasValidSerializedItems)
						writer.Write("<p class=\"warning\">Warning: at least one configuration has not serialized any items yet. Unicorn cannot operate properly until this is complete. Please review the configuration below and then perform initial serialization if it is accurate.</p>");
					else 
						writer.Write("<p class=\"warning\">Warning: your current predicate configuration for at least one configuration does not have any valid root items defined. Nothing will be serialized until valid root items to start serializing from can be resolved. Please review your predicate configuration.</p>");
				}
			}
		}
	}
}
