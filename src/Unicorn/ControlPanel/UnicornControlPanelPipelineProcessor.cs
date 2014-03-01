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
using Unicorn.Predicates;
using Unicorn.Serialization;

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

		private IConfiguration[] _configurations = UnicornConfigurationManager.Configurations;
		protected IConfiguration[] Configurations
		{
			get { return _configurations; }
			set { _configurations = value; }
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
			var hasSerializedItems = Configurations.All(config => ControlPanelUtility.HasAnySerializedItems(config.Resolve<IPredicate>(), config.Resolve<ISerializationProvider>()));
			var hasValidSerializedItems = Configurations.All(config => config.Resolve<IPredicate>().GetRootItems().Length > 0);

			var isAuthorized = Authorization.IsAllowed;

			yield return new Html5HeadAndStyles();

			var heading = new Heading();
			heading.HasSerializedItems = hasSerializedItems;
			heading.HasValidSerializedItems = hasValidSerializedItems;
			heading.IsAuthenticated = isAuthorized;
			yield return heading;

			if (isAuthorized)
			{
				if (Configurations.Length > 1 && hasSerializedItems && hasValidSerializedItems)
				{
					yield return new Literal("<h2>Global Actions</h2>");

					yield return new ControlOptions();

					yield return new Literal("<h2>Configurations</h2>");
				}

				foreach (var configuration in Configurations)
				{
					var configurationHasSerializedItems = ControlPanelUtility.HasAnySerializedItems(configuration.Resolve<IPredicate>(), configuration.Resolve<ISerializationProvider>());
					var configurationHasValidRootItems = configuration.Resolve<IPredicate>().GetRootItems().Length > 0;

					yield return new Literal("<div class=\"configuration\"><h3>{0}</h3><section>".FormatWith(configuration.Name));

					if(!configurationHasValidRootItems)
						yield return new Literal("<p class=\"warning\">This configuration's predicate cannot resolve any valid root items. This usually means it is configured to look for nonexistant paths or GUIDs. Please review your predicate configuration.</p>");
					else if (!configurationHasSerializedItems)
						yield return new Literal("<p class=\"warning\">This configuration does not currently have any valid serialized items. You cannot sync it until you perform an initial serialization.</p>");

					if (configurationHasSerializedItems)
					{
						var controlOptions = configuration.Resolve<ControlOptions>();
						controlOptions.ConfigurationName = configuration.Name;
						yield return controlOptions;
					}

					var configDetails = configuration.Resolve<ConfigurationDetails>();
					configDetails.ConfigurationName = configuration.Name;
					configDetails.CollapseByDefault = configurationHasSerializedItems;
					yield return configDetails;

					if (!configurationHasSerializedItems)
					{
						var initialSetup = configuration.Resolve<InitialSetup>();
						initialSetup.ConfigurationName = configuration.Name;
						yield return initialSetup;
					}

					yield return new Literal("</section></div>");
				}
			}
			else
			{
				yield return new AccessDenied();
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
