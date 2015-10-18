using System;
using System.Configuration;
using System.Net;
using System.Web;
using AuthenticationManager = Sitecore.Security.Authentication.AuthenticationManager;

namespace Unicorn.ControlPanel.Security
{
	public class LegacyAuthenticationProvider : IUnicornAuthenticationProvider
	{
		private static readonly string CorrectAuthToken = ConfigurationManager.AppSettings["DeploymentToolAuthToken"];

		public string GetChallengeToken()
		{
			// not using CHAP
			return string.Empty;
		}

		public SecurityState ValidateRequest(HttpRequestBase request)
		{
			var user = AuthenticationManager.GetActiveUser();

			if (user.IsAdministrator)
			{
				return new SecurityState(true, false);
			}

			var authToken = HttpContext.Current.Request.Headers["Authenticate"];

			if (!string.IsNullOrWhiteSpace(CorrectAuthToken) &&
				!string.IsNullOrWhiteSpace(authToken) &&
				authToken.Equals(CorrectAuthToken, StringComparison.Ordinal))
			{
				return new SecurityState(true, true);
			}

			// if dynamic debug compilation is enabled, you can use it without auth (eg local dev)
			if (HttpContext.Current.IsDebuggingEnabled)
				return new SecurityState(true, false);

			return new SecurityState(false, false);
		}

		public WebClient CreateAuthenticatedWebClient(string remoteUnicornUrl)
		{
			var client = new SuperWebClient(RequestTimeoutInMs);
			client.Headers.Add("Authenticate", CorrectAuthToken);

			return client;
		}

		protected virtual int RequestTimeoutInMs
		{
			get { return 1000 * 7200; /* 1000ms * 7200sec = 2 hours */ }
		}

		protected class SuperWebClient : WebClient
		{
			private readonly int _timeoutInMs;

			public SuperWebClient(int timeoutInMs)
			{
				_timeoutInMs = timeoutInMs;
			}

			protected override WebRequest GetWebRequest(Uri address)
			{
				WebRequest request = base.GetWebRequest(address);

				request.Timeout = _timeoutInMs;

				return request;
			}
		}
	}
}
