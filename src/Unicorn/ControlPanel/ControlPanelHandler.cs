using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using Kamsar.WebConsole;
using Sitecore.Security.Authentication;
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

		private IDependencyRegistry _dependencyRegistry = Registry.CreateCopyOfDefault();
		protected IDependencyRegistry DependencyRegistry 
		{
			get { return _dependencyRegistry; }
			set { _dependencyRegistry = value; } 
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

			using (var writer = new HtmlTextWriter(context.Response.Output))
			{
				foreach (var control in controls)
					control.Render(writer);
			}
		}

		protected virtual IEnumerable<IControlPanelControl> GetDefaultControls()
		{
			// bit of a hack - default config depends on the reg of one of these
			DependencyRegistry.Register<IProgressStatus>(() => new StringProgressStatus());

			var hasSerializedItems = ControlPanelUtility.HasAnySerializedItems(DependencyRegistry.Resolve<IPredicate>(), DependencyRegistry.Resolve<ISerializationProvider>());
			var isAuthorized = Authorization.IsAllowed;

			yield return DependencyRegistry.Resolve<Html5HeadAndStyles>();

			var heading = DependencyRegistry.Resolve<Heading>();
			heading.HasSerializedItems = hasSerializedItems;
			heading.IsAuthenticated = isAuthorized;
			yield return heading;

			if (isAuthorized)
			{
				if (hasSerializedItems)
				{
					yield return DependencyRegistry.Resolve<ControlOptions>();
				}

				yield return DependencyRegistry.Resolve<Configuration>();

				if (!hasSerializedItems)
				{
					yield return DependencyRegistry.Resolve<InitialSetup>();
				}
			}
			else
			{
				yield return DependencyRegistry.Resolve<AccessDenied>();
			}

			yield return DependencyRegistry.Resolve<Html5Footer>();
		}

		protected virtual IEnumerable<IControlPanelControl> GetReserializeControls(bool isAutomatedTool)
		{
			yield return new ReserializeConsole(isAutomatedTool, DependencyRegistry);
		}

		protected virtual IEnumerable<IControlPanelControl> GetSyncControls(bool isAutomatedTool)
		{
			yield return new SyncConsole(isAutomatedTool, DependencyRegistry);
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
