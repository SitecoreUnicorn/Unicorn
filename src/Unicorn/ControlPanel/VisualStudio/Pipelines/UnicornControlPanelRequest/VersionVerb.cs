using Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest;
using Unicorn.ControlPanel.Responses;

namespace Unicorn.ControlPanel.VisualStudio.Pipelines.UnicornControlPanelRequest
{
	public class VersionVerb : UnicornControlPanelRequestPipelineProcessor
	{
		public VersionVerb() : base("Version")
		{
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new PlainTextResponse(UnicornVersion.Current);
		}
	}
}
