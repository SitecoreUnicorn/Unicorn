using System.Linq;
using Unicorn.Configuration;
using Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest;
using Unicorn.ControlPanel.Responses;

namespace Unicorn.ControlPanel.VisualStudio.Pipelines.UnicornControlPanelRequest
{
	public class ConfigurationsVerb : UnicornControlPanelRequestPipelineProcessor
	{
		public ConfigurationsVerb() : base("Configurations")
		{
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new PlainTextResponse(string.Join(",", UnicornConfigurationManager.Configurations.Select(config => config.Name)));
		}
	}
}
