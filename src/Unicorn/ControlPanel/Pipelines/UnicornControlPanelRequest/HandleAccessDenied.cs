using System.Net;
using Unicorn.ControlPanel.Controls;
using Unicorn.ControlPanel.Responses;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class HandleAccessDenied : UnicornControlPanelRequestPipelineProcessor
	{
		// NOTE: because each processor checks for authentication individually this is more of an unhandled access denied handler as opposed to a gate
		// Should come before control panel in pipeline

		public HandleAccessDenied() : base(string.Empty)
		{
		}

		protected override bool HandlesVerb(UnicornControlPanelRequestPipelineArgs args)
		{
			return !args.SecurityState.IsAllowed;
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			if (args.SecurityState.IsAutomatedTool)
			{
				return new PlainTextResponse("Automated tool authentication failed.", HttpStatusCode.Unauthorized);
			}

			return new ControlPanelPageResponse(args.SecurityState, new AccessDenied());
		}
	}
}
