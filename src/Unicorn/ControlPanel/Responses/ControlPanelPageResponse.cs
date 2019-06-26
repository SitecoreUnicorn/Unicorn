using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using Sitecore.SecurityModel;
using Unicorn.ControlPanel.Controls;
using SecurityState = Unicorn.ControlPanel.Security.SecurityState;

namespace Unicorn.ControlPanel.Responses
{
	public class ControlPanelPageResponse : IResponse
	{
		private readonly SecurityState _securityState;
		private readonly IControlPanelControl[] _controls;

		public ControlPanelPageResponse(SecurityState securityState, params IControlPanelControl[] controls)
		{
			_securityState = securityState;
			_controls = controls;
		}

		public virtual void Execute(HttpResponseBase response)
		{
			response.StatusCode = 200;
			response.ContentType = "text/html";

			var masterControls = new List<IControlPanelControl>();

			masterControls.AddRange(CreateHeaderControls(_securityState));
			
			masterControls.AddRange(_controls);

			masterControls.AddRange(CreateFooterControls());

			using (var writer = new HtmlTextWriter(response.Output))
			{
				// this securitydisabler allows the control panel to execute unfettered when debug compilation is enabled but you are not signed into Sitecore
				using (new SecurityDisabler())
				{
					foreach (var control in masterControls)
						control.Render(writer);
				}
			}

			response.End();
		}

		protected virtual IEnumerable<IControlPanelControl> CreateHeaderControls(SecurityState securityState)
		{
			yield return new HtmlHeadAndStyles();

			yield return new Heading(securityState.IsAllowed);
		}

		protected virtual IEnumerable<IControlPanelControl> CreateFooterControls()
		{
			yield return new HtmlFooter();
		} 
	}
}
