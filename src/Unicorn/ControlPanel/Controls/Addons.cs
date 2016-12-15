using System.Web.UI;
using Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest;

namespace Unicorn.ControlPanel.Controls
{
	/// <summary>
	/// Addons section
	/// </summary>
	internal class Addons : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write(@"
				<article>");
			writer.Write(@"
					<h2>Addons</h2>");

			writer.Write("<section>");
			writer.Write("<table><tbody><tr>");

			writer.Write("<td>");
			writer.Write("<h3>Gutter Addon</h3>");
			writer.Write("<p>Adds a gutter icon to any items that are included in a Unicorn Transparent Sync configuration</p>");
			writer.Write(@"<p class=""help"">Note: if you have upgraded Unicorn and see a ""Verb Not Found"" message when clicking this button, make sure the following entries are present in your <strong>unicornControlPanelRequest</strong> pipeline:");
			writer.Write(@"<pre class=""help"">
&lt;processor type=""Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest.InstallGutterVerb, Unicorn"" /&gt;
&lt;processor type=""Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest.RemoveGutterVerb, Unicorn"" /&gt;
</pre>");
			writer.Write("</td>");
			writer.Write(@"<td class=""controls"">");

			if (GutterVerbBase.GetGutterItem() != null)
			{
				writer.Write(@"<a class=""button"" data-basehref=""?verb=RemoveGutter"" href=""?verb=RemoveGutter"">Remove Gutter Addon</a>");
			}
			else
			{
				writer.Write(@"<a class=""button"" data-basehref=""?verb=InstallGutter"" href=""?verb=InstallGutter"">Install Gutter Addon</a>");
			}
			writer.Write("</td>");
			writer.Write("</tbody></table>");
			writer.Write("</section>");
			writer.Write(@"
				</article>");
		}
	}
}
