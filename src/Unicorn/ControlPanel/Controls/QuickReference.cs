using System.Web.UI;
using Sitecore.Configuration;

namespace Unicorn.ControlPanel.Controls
{
	/// <summary>
	/// Quick reference about what commands mean
	/// </summary>
	internal class QuickReference : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write(@"
				<article>");
			writer.Write(@"
					<h2>Reference</h2>");

			writer.Write("<section>");
			writer.Write("<h3>What is Sync?</h3>");
			writer.Write("<p>Runs a synchronization operation, which will sync serialized items into Sitecore. The serialized state is merged into the database state, making additions, modifications, and deletions (in some cases) as needed.</p>");
			writer.Write("<p class=\"help\">Note: if you only need to sync part of a configuration, you can use the <em>Update Tree</em> command on the Developer tab of the Sitecore ribbon. When used on a Unicorn item, it performs a partial sync.");
			writer.Write("</section>");

			writer.Write("<section>");
			writer.Write("<h3>What is Reserialize?</h3>");
			writer.Write("<p>This clears the serialized data and refills it with the items in the Sitecore database. This should be done if you add or remove items from your predicate after initial serialization.</p>");
			writer.Write("<p class=\"help\">Note: if you only need to reserialize part of a configuration, you can use the <em>Dump Tree</em> command on the Developer tab of the Sitecore ribbon. When used on a Unicorn item, it performs a partial reserialize.");
			writer.Write("</section>");

			writer.Write("<section>");
			writer.Write("<h3>Documentation</h3>");
			writer.Write("<p>Looking for something else? Here are some places to get help.</p>");
			writer.Write(@"
<ul>
	<li><a href=""https://github.com/kamsar/Unicorn/blob/master/Build/Unicorn.nuget/readme.txt"">Unicorn Getting Started/Installation</a></li>
	<li><a href=""https://github.com/kamsar/Unicorn/blob/master/README.md"">Unicorn Quick Start</a></li>
	<li><a href=""https://unicorn.kamsar.net/"">Unicorn Book</a></li>
	<li><a href=""https://github.com/kamsar/Unicorn/tree/master/src/Unicorn/Standard%20Config%20Files"">Default Configuration Files</a></li>
	<li><a href=""http://kamsar.net/index.php/category/Unicorn/"">Unicorn Blog Posts</a></li>
	<li><a href=""https://visualstudiogallery.msdn.microsoft.com/64439022-f470-422a-b663-fbb89aaf6e86"">Unicorn Control Panel for Visual Studio</a></li>
	<li><a href=""https://github.com/kamsar/Unicorn/issues/new?title=Issue with Unicorn {0} on Sitecore {1}&amp;body=Please include what you were doing before the issue occurred, complete text of any error messages, and if appropriate your Unicorn configuration setup. Thanks!"">Found a problem? Report an issue on GitHub.</a></li>
</ul>
", UnicornVersion.Current, About.Version);
			writer.Write("</section>");

			writer.Write(@"
				</article>");
		}
	}
}
