using Unicorn.Configuration;
using Unicorn.ControlPanel.Responses;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class ChallengeVerb : UnicornControlPanelRequestPipelineProcessor
	{
		public ChallengeVerb() : base("Challenge", requireAuthentication: false)
		{
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new PlainTextResponse(UnicornConfigurationManager.AuthenticationProvider.GetChallengeToken());
		}
	}
}
