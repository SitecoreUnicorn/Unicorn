using System;
using System.Web;
using Kamsar.WebConsole;
using Unicorn.Configuration;
using Unicorn.ControlPanel.Headings;
using Unicorn.ControlPanel.Responses;
using Unicorn.Logging;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class ReserializeVerb : UnicornControlPanelRequestPipelineProcessor
	{
		private readonly SerializationHelper _helper;

		public ReserializeVerb() : this("Reserialize", new SerializationHelper())
		{
		}

		protected ReserializeVerb(string verb, SerializationHelper helper) : base(verb)
		{
			_helper = helper;
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new WebConsoleResponse("Reserialize Unicorn", args.SecurityState.IsAutomatedTool, new HeadingService(), progress => Process(progress, new WebConsoleLogger(progress, args.Context.Request.QueryString["log"])));
		}

		protected virtual void Process(IProgressStatus progress, ILogger additionalLogger)
		{
			var configurations = ResolveConfigurations();

			_helper.ReserializeConfigurations(configurations, progress, additionalLogger);
		}

		protected virtual IConfiguration[] ResolveConfigurations()
		{
			// This logic is present in all verbs. Marked for refactoring
			var config = HttpContext.Current.Request.QueryString["configuration"];
			var exclude = HttpContext.Current.Request.QueryString["exclude"];
			var targetConfigurations = ControlPanelUtility.ResolveConfigurationsFromQueryParameter(config, exclude);

			if (targetConfigurations.Length == 0) throw new ArgumentException("Configuration(s) requested were not defined.");

			return targetConfigurations;
		}
	}
}
