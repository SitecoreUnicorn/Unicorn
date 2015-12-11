using System.Web.UI;

namespace Unicorn.ControlPanel.Controls
{
	internal class GlobalWarnings : IControlPanelControl
	{
		private readonly bool _hasAllRootPaths;
		private readonly bool _hasDependencies;

		public GlobalWarnings(bool hasAllRootPaths, bool hasDependencies)
		{
			_hasAllRootPaths = hasAllRootPaths;
			_hasDependencies = hasDependencies;
		}

		public void Render(HtmlTextWriter writer)
		{
			if (_hasAllRootPaths)
				writer.Write("<p class=\"warning\">Warning: at least one configuration has not serialized any items yet. Unicorn cannot operate properly until this is complete. Please review the configuration below and then perform initial serialization if it is accurate.</p>");
			else
				writer.Write("<p class=\"warning\">Warning: your current predicate configuration for at least one configuration does not have any valid root items defined. Nothing will be serialized until valid root items to start serializing from can be resolved. Please review your predicate configuration.</p>");

			if(_hasDependencies)
				writer.Write(@"<p class=""warning"">There are dependencies between some configurations, therefore the order in which you sync them is important.</p>");
		}
	}
}
