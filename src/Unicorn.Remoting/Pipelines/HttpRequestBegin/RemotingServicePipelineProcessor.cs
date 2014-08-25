using System;
using System.Configuration;
using System.Web;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Security.Authentication;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;
using Unicorn.Configuration;

namespace Unicorn.Remoting.Pipelines.HttpRequestBegin
{
	/// <summary>
	/// This is a httpRequestBegin pipeline processor that is effectively a sitecore-integrated HTTP handler.
	/// It serves the Unicorn remoting API if the current URL matches the activationUrl.
	/// </summary>
	public class RemotingServicePipelineProcessor : HttpRequestProcessor
	{
		private readonly string _activationUrl;

		public RemotingServicePipelineProcessor(string activationUrl)
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
			context.Server.ScriptTimeout = 3600;

			string configurationName = context.Request.QueryString["c"];
			string timestamp = context.Request.QueryString["ts"];

			DateTime lastUpdated;

			if (timestamp == null || !DateTime.TryParse(timestamp, out lastUpdated))
			{
				context.Response.StatusCode = 400;
				context.Response.Write("Invalid timestamp.");
				context.Response.End();
				return;
			}

			if (configurationName.IsNullOrEmpty())
			{
				context.Response.StatusCode = 400;
				context.Response.Write("Missing configuration.");
				context.Response.End();
			}

			/*if (!Authorization.IsAllowed)
			{
				context.Response.StatusCode = 403;
				context.Response.Write("Access denied.");
				context.Response.End();
			}
			else*/
			{
				// this securitydisabler allows the control panel to execute unfettered when debug compilation is enabled but you are not signed into Sitecore
				using (new SecurityDisabler())
				{
					// TODO: check security (HMAC)

					using (var package = new RemotingService().CreateRemotingPackage(configurationName, lastUpdated))
					{
						package.WriteToHttpResponse(new HttpResponseWrapper(context.Response));
					}
				}
			}
		}

		protected virtual bool Authorization
		{
			get
			{
				var user = AuthenticationManager.GetActiveUser();

				if (user.IsAdministrator)
				{
					return true;
				}

				var authToken = HttpContext.Current.Request.Headers["Authenticate"];
				var correctAuthToken = ConfigurationManager.AppSettings["DeploymentToolAuthToken"];

				if (!string.IsNullOrWhiteSpace(correctAuthToken) &&
					!string.IsNullOrWhiteSpace(authToken) &&
					authToken.Equals(correctAuthToken, StringComparison.Ordinal))
				{
					return true;
				}

				// if dynamic debug compilation is enabled, you can use it without auth (eg local dev)
				if (HttpContext.Current.IsDebuggingEnabled)
					return true;

				return false;
			}
		}
	}
}
