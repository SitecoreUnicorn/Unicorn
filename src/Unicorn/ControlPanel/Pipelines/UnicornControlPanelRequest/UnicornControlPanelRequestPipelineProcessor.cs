using System;
using Unicorn.ControlPanel.Responses;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public abstract class UnicornControlPanelRequestPipelineProcessor
	{
		private readonly string _verbHandled;
		private readonly bool _requireAuthentication;
		private readonly bool _abortPipelineIfHandled;

		protected UnicornControlPanelRequestPipelineProcessor(string verbHandled, bool requireAuthentication = true, bool abortPipelineIfHandled = true)
		{
			_verbHandled = verbHandled;
			_requireAuthentication = requireAuthentication;
			_abortPipelineIfHandled = abortPipelineIfHandled;
		}

		public virtual void Process(UnicornControlPanelRequestPipelineArgs args)
		{
			bool handled = HandlesVerb(args);

			if (!handled) return;

			args.Response = CreateResponse(args);

			if(_abortPipelineIfHandled) args.AbortPipeline();
		}

		protected virtual bool HandlesVerb(UnicornControlPanelRequestPipelineArgs args)
		{
			if (_requireAuthentication && !args.SecurityState.IsAllowed) return false;

			if (string.IsNullOrWhiteSpace(_verbHandled)) return true;

			return _verbHandled.Equals(args.Verb, StringComparison.OrdinalIgnoreCase);
		}

		protected abstract IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args);
	}
}
