using System.Web.UI;

namespace Unicorn.ControlPanel
{
	public class Html5Footer : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<script src=\"//ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js\"></script>");
			writer.Write(@"<script>
							jQuery(function() {
								$('h4.expand').click(function() {
									$(this).removeClass('expand').next('.details').slideDown();
								});
							});
						</script>");
			writer.Write(" </body></html>");
		}
	}
}
