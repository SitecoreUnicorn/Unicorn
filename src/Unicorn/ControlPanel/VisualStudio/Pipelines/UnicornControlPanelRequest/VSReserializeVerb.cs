using Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest;
using Unicorn.ControlPanel.Responses;
using Unicorn.ControlPanel.VisualStudio.Responses;

namespace Unicorn.ControlPanel.VisualStudio.Pipelines.UnicornControlPanelRequest
{
	// ReSharper disable once InconsistentNaming
	public class VSReserializeVerb : ReserializeVerb
	{
		public VSReserializeVerb() : base("VSReserialize")
		{
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new StreamingEncodedLogResponse(Process);
		}
	}
}
