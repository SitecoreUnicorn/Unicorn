using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using Kamsar.WebConsole;
using Sitecore.Security.Authentication;
using Sitecore.SecurityModel;
using Unicorn.Dependencies;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.ControlPanel
{
	public class ControlPanelHandler : IHttpHandler, IRequiresSessionState
	{
		public bool IsReusable
		{
			get { return false; }
		}

		private IConfiguration[] _configurations = UnicornConfigurationManager.Configurations;
		protected IConfiguration[] Configurations
		{
			get { return _configurations; }
			set { _configurations = value; }
		}

		public void ProcessRequest(HttpContext context)
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
			var hasSerializedItems = Configurations.Any(config => ControlPanelUtility.HasAnySerializedItems(config.Resolve<IPredicate>(), config.Resolve<ISerializationProvider>()));
			var hasValidSerializedItems = Configurations.Any(config => config.Resolve<IPredicate>().GetRootItems().Length > 0);

			var isAuthorized = Authorization.IsAllowed;

			yield return new Html5HeadAndStyles();

			var heading = new Heading();
			heading.HasSerializedItems = hasSerializedItems;
			heading.HasValidSerializedItems = hasValidSerializedItems;
			heading.IsAuthenticated = isAuthorized;
			yield return heading;

			foreach (var configuration in Configurations)
			{
				if (isAuthorized)
				{
					if (hasSerializedItems)
					{
						yield return configuration.Resolve<ControlOptions>();
					}

					yield return configuration.Resolve<Configuration>();

					if (!hasSerializedItems)
					{
						yield return configuration.Resolve<InitialSetup>();
					}
				}
				else
				{
					yield return configuration.Resolve<AccessDenied>();
				}


			}

			yield return new Html5Footer();
		}

		protected virtual IEnumerable<IControlPanelControl> GetReserializeControls(bool isAutomatedTool)
		{
			yield return new ReserializeConsole(isAutomatedTool, Configurations);
		}

		protected virtual IEnumerable<IControlPanelControl> GetSyncControls(bool isAutomatedTool)
		{
			yield return new SyncConsole(isAutomatedTool, Configurations);
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
