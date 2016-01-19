using System.Collections.Generic;
using System.Linq;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Configuration.Dependencies;
using Unicorn.ControlPanel.Controls;
using Unicorn.ControlPanel.Responses;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class RenderControlPanel : UnicornControlPanelRequestPipelineProcessor
	{
		public RenderControlPanel() : base(string.Empty)
		{
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new ControlPanelPageResponse(args.SecurityState, CreateBodyControls(args).ToArray());
		}

		protected virtual IEnumerable<IControlPanelControl> CreateBodyControls(UnicornControlPanelRequestPipelineArgs args)
		{
			var configurations = GetConfigurations(args);

			var hasSerializedItems = configurations.All(ControlPanelUtility.HasAnySerializedItems);
			var hasAllRootPaths = configurations.All(ControlPanelUtility.AllRootPathsExist);
			var allowMultiSelect = hasSerializedItems && hasAllRootPaths && configurations.Length > 1;
			// note that we don't just check dependencies property here to catch implicit dependencies
			var anyConfigurationsWithDependencies = configurations.Any(config => config.Resolve<ConfigurationDependencyResolver>().Dependents.Any());

			var isAuthorized = args.SecurityState.IsAllowed;

			if (!hasSerializedItems)
			{
				yield return new GlobalWarnings(hasAllRootPaths, anyConfigurationsWithDependencies);
			}

			if (isAuthorized)
			{
				if (configurations.Length == 0)
				{
					yield return new NoConfigurations();
					yield break;
				}

				if (configurations.Length > 1 && hasSerializedItems && hasAllRootPaths)
				{
					yield return new BatchProcessingControls();
				}

				yield return new Literal(@"
						<article>
							<h2{0} Configurations</h2>".FormatWith(allowMultiSelect ? @" class=""fakebox fakebox-all""><span></span>" : ">"));

				if (allowMultiSelect) yield return new Literal(@"
							<p class=""help"">Check 'Configurations' above to select all configurations, or individually select as many as you like below.</p>");

				yield return new Literal(@"
							<p class=""help"">Expecting a huge number of changes? Try Quiet mode, which only logs warnings and errors and runs faster for large changesets. Add '&quiet=1' to any sync or reserialize URL to activate. Sitecore logs will still receive full detail.</p>");

				yield return new Literal(@"
							<table>
								<tbody>");

				foreach (var configuration in configurations)
				{
					yield return new ConfigurationInfo(configuration) { MultipleConfigurationsExist = allowMultiSelect };
				}

				yield return new Literal(@"
								</tbody>
							</table>
						</article>");

				yield return new QuickReference();
			}
			else
			{
				yield return new AccessDenied();
			}
		}

		protected virtual IConfiguration[] GetConfigurations(UnicornControlPanelRequestPipelineArgs args)
		{
			return new InterconfigurationDependencyResolver().OrderByDependencies(UnicornConfigurationManager.Configurations);
		}
	}
}
