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

		protected bool IsOrderedByDependents(HttpContext context)
		{
			return context.Request.QueryString["order"] != "Config";
		}

		protected IConfiguration[] GetConfigurations(HttpContext context)
		{
			if (IsOrderedByDependents(context))
				return UnicornConfigurationManager.GetConfigurationsOrdererdByDependents();
			return UnicornConfigurationManager.Configurations;
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

			IEnumerable<IControlPanelControl> controls;
			var configurations = GetConfigurations(context);

			if (!Authorization.IsAllowed)
			{
				controls = GetDefaultControls(context, configurations);
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
							controls = GetDefaultControls(context, configurations);
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

		protected virtual IEnumerable<IControlPanelControl> GetDefaultControls(HttpContext context, IConfiguration[] configurations)
		{
			context.Response.AddHeader("Content-Type", "text/html");

			var hasSerializedItems = configurations.All(ControlPanelUtility.HasAnySerializedItems);
			var hasAllRootPaths = configurations.All(ControlPanelUtility.AllRootPathsExists);
			var allowMultiSelect = hasSerializedItems && hasAllRootPaths && configurations.Length > 1;
			var anyConfigurationsWithDependencies = configurations.Any(ControlPanelUtility.HasDependents);

			var isAuthorized = Authorization.IsAllowed;

			yield return new Html5HeadAndStyles();

			var heading = new Heading
			              {
				              HasSerializedItems = hasSerializedItems,
				              HasAllRootPaths = hasAllRootPaths,
				              IsAuthenticated = isAuthorized
			              };
			yield return heading;

			if (isAuthorized)
			{
				if (configurations.Length == 0)
				{
					yield return new NoConfigurations();
					yield break;
				}

				if (configurations.Length > 1 && hasSerializedItems && hasAllRootPaths)
				{
					yield return new BatchProcessingControls();
				}

				yield return new Literal(@"
						<article>
							<h2{0} Configurations</h2>".FormatWith(allowMultiSelect ? @" class=""fakebox fakebox-all""><span></span>" : ">"));

				if (anyConfigurationsWithDependencies)
				{
					yield return new Literal(@"
							<div class=""warning""><p>There are dependencies between the configurations and therefore the order of synchronization might be important.</p>");

					if (IsOrderedByDependents(context))
					{
						yield return new Literal(@"
								<p><em>The configurations are shown in order of dependencies.</em></p>
								<a class=""button"" href=""?order=Config"">Order by configuration</a>");
					}
					else
					{
						yield return new Literal(@"
								<p><em>The configurations are listed as they are in the configuration file.</em></p>
								<a class=""button"" href =""?order="">Order by dependencies</a>");
					}

					yield return new Literal(@"
							</div>");
				}

				if (allowMultiSelect) yield return new Literal(@"
							<p class=""help"">Check 'Configurations' above to select all configurations, or individually select as many as you like below.</p>");

				yield return new Literal(@"
							<p class=""help"">Expecting a huge number of changes? Try Quiet mode, which only logs warnings and errors and runs faster for large changesets. Add '&quiet=1' to any sync or reserialize URL to activate. Sitecore logs will still receive full detail.</p>");

				yield return new Literal(@"
							<table>
								<tbody>");

				foreach (var configuration in configurations)
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
