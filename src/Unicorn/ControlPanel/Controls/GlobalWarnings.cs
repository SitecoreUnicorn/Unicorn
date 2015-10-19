using System.Web.UI;

namespace Unicorn.ControlPanel.Controls
{
	internal class GlobalWarnings : IControlPanelControl
	{
		private readonly bool _hasValidSerializedItems;

		public GlobalWarnings(bool hasValidSerializedItems)
		{
			_hasValidSerializedItems = hasValidSerializedItems;
		}

		public void Render(HtmlTextWriter writer)
		{
			if (_hasValidSerializedItems)
				writer.Write("<p class=\"warning\">Warning: at least one configuration has not serialized any items yet. Unicorn cannot operate properly until this is complete. Please review the configuration below and then perform initial serialization if it is accurate.</p>");
			else
				writer.Write("<p class=\"warning\">Warning: your current predicate configuration for at least one configuration does not have any valid root items defined. Nothing will be serialized until valid root items to start serializing from can be resolved. Please review your predicate configuration.</p>");
		}
	}
}
