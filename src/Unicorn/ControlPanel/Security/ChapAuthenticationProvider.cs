using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using MicroCHAP;
using MicroCHAP.Server;
using Sitecore.Exceptions;
using AuthenticationManager = Sitecore.Security.Authentication.AuthenticationManager;

namespace Unicorn.ControlPanel.Security
{
	public class ChapAuthenticationProvider : IUnicornAuthenticationProvider
	{
		private static IChapServer _server;
		private static ISignatureService _signatureService;

		public string SharedSecret { get; set; }
		public string ChallengeDatabase { get; set; } = "web";

		protected virtual IChapServer Server
		{
			get
			{
				if (_server == null)
					_server = new ChapServer(SignatureService, ChallengeStore);
				
				return _server;
			}
		}

		protected virtual ISignatureService SignatureService
		{
			get
			{
				// if no shared secret is set, we set a random double. This essentially renders it unusable.
				// this is done so that we can allow people to use Unicorn for non-tool access without setting a shared secret.
				// we verify that the shared secret is not numeric before actually using it for tool authentication.
				if(string.IsNullOrWhiteSpace(SharedSecret)) SharedSecret = new Random().NextDouble().ToString(CultureInfo.InvariantCulture);

				if(_signatureService == null)
					_signatureService = new SignatureService(SharedSecret);

				return _signatureService;
			}
		}

		protected virtual IChallengeStore ChallengeStore => new SitecoreDatabaseChallengeStore(ChallengeDatabase);

		public string GetChallengeToken()
		{
			ValidateSharedSecret();
			return Server.GetChallengeToken();
		}

		public SecurityState ValidateRequest(HttpRequestBase request)
		{
			var user = AuthenticationManager.GetActiveUser();

			if (user.IsAdministrator)
			{
				return new SecurityState(true, false);
			}

			var authToken = request.Headers["X-MC-MAC"];

			if (!string.IsNullOrWhiteSpace(authToken))
			{
				ValidateSharedSecret();

				if (_server.ValidateRequest(request))
				{
					return new SecurityState(true, true);
				}

				return new SecurityState(false, true);
			}

			// if dynamic debug compilation is enabled, you can use it without auth (eg local dev)
			if (HttpContext.Current.IsDebuggingEnabled)
				return new SecurityState(true, false);

			return new SecurityState(false, false);
		}

		public WebClient CreateAuthenticatedWebClient(string remoteUnicornUrl)
		{
			var remoteUri = new Uri(remoteUnicornUrl);

			var client = new SuperWebClient(RequestTimeoutInMs);

			// we get a new challenge from the remote Unicorn, which is a unique known value to both parties
			var challenge = client.DownloadString(remoteUri.GetLeftPart(UriPartial.Path) + "?verb=Challenge");

			// then we sign the request using our shared secret combined with the challenge and the URL, providing a unique verifiable hash for the request
			client.Headers.Add("X-MC-MAC", _signatureService.CreateSignature(challenge, remoteUnicornUrl, Enumerable.Empty<SignatureFactor>()));
			
			// the Unicorn server needs to know the challenge we are using. It makes sure that it issued the challenge before validating it.
			client.Headers.Add("X-MC-Nonce", challenge);

			return client;
		}

		protected virtual int RequestTimeoutInMs => 1000 * 60 * 120; // 2h in msec

		protected virtual void ValidateSharedSecret()
		{
			if (string.IsNullOrWhiteSpace(SharedSecret))
				throw new SecurityException("The Unicorn shared secret is not set. Add a child <SharedSecret> element in the Unicorn <authenticationProvider> config (Unicorn.UI.config) and set a secure shared secret, e.g. a 64-char random string.");

			double secCheck;
			if (double.TryParse(SharedSecret, out secCheck)) // if no shared secret is set we make it a random double, but we reject that once you actually try to authenticate with a tool
				throw new SecurityException("The Unicorn shared secret is not set, or was set to a numeric value. Add a child <SharedSecret> element in the Unicorn <authenticationProvider> config (Unicorn.UI.config) and set a secure shared secret, e.g. a 64-char random string.");

			if (SharedSecret.Length < 30) throw new SecurityException("Your Unicorn shared secret is not long enough. Please make it more than 30 characters for maximum security. You can set this in Unicorn.UI.config on the <authenticationProvider>");
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
