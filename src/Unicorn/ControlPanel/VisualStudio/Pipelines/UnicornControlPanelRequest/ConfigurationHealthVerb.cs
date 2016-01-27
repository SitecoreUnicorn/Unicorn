using System;
using System.Linq;
using Unicorn.Configuration;
using Unicorn.Configuration.Dependencies;
using Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest;
using Unicorn.ControlPanel.Responses;
using Unicorn.Data.DataProvider;

namespace Unicorn.ControlPanel.VisualStudio.Pipelines.UnicornControlPanelRequest
{
	public class ConfigurationHealthVerb : UnicornControlPanelRequestPipelineProcessor
	{
		public ConfigurationHealthVerb() : base("ConfigurationHealth")
		{

		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			var config = args.Context.Request.QueryString["configuration"];
			var targetConfigurations = ControlPanelUtility.ResolveConfigurationsFromQueryParameter(config);
			var result = targetConfigurations.Select(GetHealthStatus);

			return new PlainTextResponse(string.Join(Environment.NewLine, result));
		}

		private string GetHealthStatus(IConfiguration configuration)
		{
			var configurationHasAnySerializedItems = ControlPanelUtility.HasAnySerializedItems(configuration);
			var configurationHasValidRootPaths = ControlPanelUtility.AllRootPathsExist(configuration);
			var unicornDataProviderConfiguration = configuration.Resolve<IUnicornDataProviderConfiguration>();
			var configurationHasTransparentSync = unicornDataProviderConfiguration != null && unicornDataProviderConfiguration.EnableTransparentSync;
			var dependents = configuration.Resolve<ConfigurationDependencyResolver>().Dependencies;
			var dependentsData = string.Join(", ", dependents.Select(d => d.Configuration.Name));
			return $"{configuration.Name}|{configurationHasAnySerializedItems}|{configurationHasValidRootPaths}|{configurationHasTransparentSync}|{dependentsData}";
		}
	}
}