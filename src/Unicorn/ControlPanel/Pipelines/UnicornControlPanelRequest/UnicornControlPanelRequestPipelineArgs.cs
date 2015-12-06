using System.Web;
using Sitecore.Pipelines;
using Unicorn.ControlPanel.Responses;
using Unicorn.ControlPanel.Security;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class UnicornControlPanelRequestPipelineArgs : PipelineArgs
	{
		public string Verb { get; private set; }

		public HttpContextBase Context { get; private set; }

		public SecurityState SecurityState { get; private set; }

		public IResponse Response { get; set; }

		public UnicornControlPanelRequestPipelineArgs(string verb, HttpContextBase context, SecurityState securityState)
		{
			Verb = verb;
			Context = context;
			SecurityState = securityState;
		}
	}
}
