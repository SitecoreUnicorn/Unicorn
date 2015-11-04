using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Security.Authentication;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.ControlPanel.Controls;

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

	    protected IConfiguration[] Configurations { get; set; } = UnicornConfigurationManager.GetConfigurationsOrdererdByDependants();

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

			IEnumerable<IControlPanelControl> controls;

			if (!Authorization.IsAllowed)
			{
				controls = GetDefaultControls();
			}
			else
			{
				// this securitydisabler allows the control panel to execute unfettered when debug compilation is enabled but you are not signed into Sitecore
				using (new SecurityDisabler())
				{
					var verb = context.Request.QueryString["verb"];

					switch (verb)
					{
						case "Sync":
							controls = GetSyncControls(Authorization.IsAutomatedTool);
							break;
						case "Reserialize":
							controls = GetReserializeControls(Authorization.IsAutomatedTool);
							break;
						default:
							controls = GetDefaultControls();
							break;
					}
				}
			}

			using (var writer = new HtmlTextWriter(context.Response.Output))
			{
				// this securitydisabler allows the control panel to execute unfettered when debug compilation is enabled but you are not signed into Sitecore
				using (new SecurityDisabler())
				{
					foreach (var control in controls)
						control.Render(writer);
				}
			}
		}

		protected virtual IEnumerable<IControlPanelControl> GetDefaultControls()
		{
			HttpContext.Current.Response.AddHeader("Content-Type", "text/html");

			var hasSerializedItems = Configurations.All(ControlPanelUtility.HasAnySerializedItems);
			var hasAllRootPaths = Configurations.All(ControlPanelUtility.AllRootPathsExists);
			var allowMultiSelect = hasSerializedItems && hasAllRootPaths && Configurations.Length > 1;

			var isAuthorized = Authorization.IsAllowed;

			yield return new Html5HeadAndStyles();

			var heading = new Heading();
			heading.HasSerializedItems = hasSerializedItems;
			heading.HasAllRootPaths = hasAllRootPaths;
			heading.IsAuthenticated = isAuthorized;
			yield return heading;

			if (isAuthorized)
			{
				if (Configurations.Length == 0)
				{
					yield return new NoConfigurations();
					yield break;
				}

				if (Configurations.Length > 1 && hasSerializedItems && hasAllRootPaths)
				{
					yield return new BatchProcessingControls();
				}

				yield return new Literal(@"
						<article>
							<h2{0} Configurations</h2>".FormatWith(allowMultiSelect ? @" class=""fakebox fakebox-all""><span></span>" : ">"));

				if (allowMultiSelect) yield return new Literal(@"
							<p class=""help"">Check 'Configurations' above to select all configurations, or individually select as many as you like below.</p>");

				yield return new Literal(@"
							<p class=""help"">Expecting a huge number of changes? Try Quiet mode, which only logs warnings and errors and runs faster for large changesets. Add '&quiet=1' to any sync or reserialize URL to activate. Sitecore logs will still receive full detail.</p>");

				yield return new Literal(@"
							<table>
								<tbody>");

				foreach (var configuration in Configurations)
				{
					yield return new ConfigurationInfo(configuration) { MultipleConfigurationsExist = allowMultiSelect };
				}

				yield return new Literal(@"
								</tbody>
							</table>
						</article>");

				yield return new QuickReference();
			}
			else
			{
				yield return new AccessDenied();
			}

			yield return new Html5Footer();
		}

		protected virtual IEnumerable<IControlPanelControl> GetReserializeControls(bool isAutomatedTool)
		{
			yield return new ReserializeConsole(isAutomatedTool);
		}

		protected virtual IEnumerable<IControlPanelControl> GetSyncControls(bool isAutomatedTool)
		{
			yield return new SyncConsole(isAutomatedTool);
		}

		protected virtual SecurityState Authorization
		{
			get
			{
				var user = AuthenticationManager.GetActiveUser();

				if (user.IsAdministrator)
				{
					return new SecurityState(true, false);
				}

				var authToken = HttpContext.Current.Request.Headers["Authenticate"];
				var correctAuthToken = ConfigurationManager.AppSettings["DeploymentToolAuthToken"];

				if (!string.IsNullOrWhiteSpace(correctAuthToken) &&
					!string.IsNullOrWhiteSpace(authToken) &&
					authToken.Equals(correctAuthToken, StringComparison.Ordinal))
				{
					return new SecurityState(true, true);
				}

				// if dynamic debug compilation is enabled, you can use it without auth (eg local dev)
				if (HttpContext.Current.IsDebuggingEnabled)
					return new SecurityState(true, false);

				return new SecurityState(false, false);
			}
		}

		protected class SecurityState
		{
			public SecurityState(bool allowed, bool automated)
			{
				IsAllowed = allowed;
				IsAutomatedTool = automated;
			}

			public bool IsAllowed { get; private set; }
			public bool IsAutomatedTool { get; private set; }
		}


	}
}
