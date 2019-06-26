using MicroCHAP;
using MicroCHAP.Server;
using Sitecore.Diagnostics;

namespace Unicorn.ControlPanel.Security
{
	public class ChapServerSitecoreLogger : IChapServerLogger
	{
		private readonly string _logPrefix;

		public ChapServerSitecoreLogger(string logPrefix)
		{
			_logPrefix = logPrefix;
		}

		public void RejectedDueToMissingHttpHeaders()
		{
			//Log.Warn($"[{_logPrefix}] CHAP authentication attempt rejected due to missing HTTP headers.", this);
		}

		public void RejectedDueToInvalidChallenge(string challengeProvided, string url)
		{
			Log.Warn($"[{_logPrefix}] CHAP authentication attempt rejected due to expired or unknown challenge value.", this);
		}

		public void RejectedDueToInvalidSignature(string challenge, string signatureProvided, SignatureResult signatureExpected)
		{
			Log.Warn($"[{_logPrefix}] CHAP authentication attempt rejected due to mismatching HMAC code.", this);
			Log.Warn($"[{_logPrefix}] MAC (should match client): {signatureExpected.SignatureSource}", this);
			Log.Warn($"[{_logPrefix}] HMAC expected: {signatureExpected.SignatureHash}", this);
			Log.Warn($"[{_logPrefix}] HMAC provided by client: {signatureProvided}", this);
			
			
		}
	}
}
