using Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest;
using Unicorn.ControlPanel.Responses;
using Unicorn.ControlPanel.VisualStudio.Responses;

namespace Unicorn.ControlPanel.VisualStudio.Pipelines.UnicornControlPanelRequest
{
	// ReSharper disable once InconsistentNaming
	public class VSSyncVerb : SyncVerb
	{
		public VSSyncVerb() : base("VSSync", new SerializationHelper())
		{
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new StreamingEncodedLogResponse(Process);
		}
	}
}
