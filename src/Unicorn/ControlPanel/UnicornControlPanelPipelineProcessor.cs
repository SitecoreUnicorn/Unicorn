using System;
using System.Net;
using System.Web;
using Sitecore.Pipelines;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.SecurityModel;
using Unicorn.Configuration;
using Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest;
using Unicorn.ControlPanel.Responses;
using SecurityState = Unicorn.ControlPanel.Security.SecurityState;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// This is a httpRequestBegin pipeline processor that is effectively a sitecore-integrated HTTP handler.
	/// It renders the Unicorn control panel UI if the current URL matches the activationUrl.
	/// </summary>
	public class UnicornControlPanelPipelineProcessor : HttpRequestProcessor
	{
		private readonly string _activationUrl;

		public UnicornControlPanelPipelineProcessor(string activationUrl)
		{
			_activationUrl = activationUrl;
		}

		protected bool IsOrderedByDependents(HttpContext context)
		{
			return context.Request.QueryString["order"] != "Config";
		}

		public override void Process(HttpRequestArgs args)
		{
			if (string.IsNullOrWhiteSpace(_activationUrl)) return;

			if (args.Context.Request.RawUrl.StartsWith(_activationUrl, StringComparison.OrdinalIgnoreCase))
			{
				ProcessRequest(args.Context);
				args.Context.Response.End();
			}
		}

		public virtual void ProcessRequest(HttpContext context)
		{
			context.Server.ScriptTimeout = 86400;

			var verb = context.Request.QueryString["verb"];

			var authProvider = UnicornConfigurationManager.AuthenticationProvider;
			SecurityState securityState;
			if (authProvider != null)
			{
				securityState = UnicornConfigurationManager.AuthenticationProvider.ValidateRequest(new HttpRequestWrapper(HttpContext.Current.Request));
			}
			else securityState = new SecurityState(false, false);

			// this securitydisabler allows the control panel to execute unfettered when debug compilation is enabled but you are not signed into Sitecore
			using (new SecurityDisabler())
			{
				var pipelineArgs = new UnicornControlPanelRequestPipelineArgs(verb, new HttpContextWrapper(context), securityState);

				CorePipeline.Run("unicornControlPanelRequest", pipelineArgs, true);

				if (pipelineArgs.Response == null)
				{
					pipelineArgs.Response = new PlainTextResponse("Not Found", HttpStatusCode.NotFound);
				}

				if (securityState.IsAllowed)
				{
					context.Response.AddHeader("X-Unicorn-Version", UnicornVersion.Current);
				}
				
				pipelineArgs.Response.Execute(new HttpResponseWrapper(context.Response));
			}
		}
	}
}
