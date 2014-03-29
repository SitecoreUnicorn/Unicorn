using System.Web;
using System.Web.UI;

namespace Unicorn.ControlPanel
{
	public class AccessDenied : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h4>Access Denied</h4>");
			writer.Write("<p>You need to <a href=\"/sitecore/admin/login.aspx?ReturnUrl={0}\">sign in to Sitecore as an administrator</a> to use the Unicorn control panel.</p>", HttpUtility.UrlEncode(HttpContext.Current.Request.Url.PathAndQuery));

			HttpContext.Current.Response.TrySkipIisCustomErrors = true;
			HttpContext.Current.Response.StatusCode = 401;
		}
	}
}
