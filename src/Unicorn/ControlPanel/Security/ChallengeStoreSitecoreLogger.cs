using MicroCHAP.Server;
using Sitecore.Diagnostics;

namespace Unicorn.ControlPanel.Security
{
	public class ChallengeStoreSitecoreLogger : IChallengeStoreLogger
	{
		private readonly string _logPrefix;

		public ChallengeStoreSitecoreLogger(string logPrefix)
		{
			_logPrefix = logPrefix;
		}

		public void ChallengeUnknown(string challenge)
		{
			Log.Warn($"[{_logPrefix}] CHAP challenge store rejected {challenge} because it was unknown.", this);
		}

		public void ChallengeExpired(string challenge)
		{
			Log.Warn($"[{_logPrefix}] CHAP challenge store removed expired challenge {challenge}.", this);
		}
	}
}
