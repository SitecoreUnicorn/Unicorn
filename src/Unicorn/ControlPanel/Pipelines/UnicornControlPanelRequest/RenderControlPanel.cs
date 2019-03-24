using System.Collections.Generic;
using System.Linq;
using Sitecore.ContentSearch.Utilities;
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
			var hasAllRootParentPaths = configurations.All(ControlPanelUtility.AllRootParentPathsExist);
			var allowMultiSelect = hasSerializedItems && hasAllRootParentPaths && configurations.Length > 1;
			var allowReserializeAll = hasAllRootParentPaths && configurations.Sum(c => ControlPanelUtility.GetInvalidRootPaths(c).Length) == 0;

			// note that we don't just check dependencies property here to catch implicit dependencies
			var anyConfigurationsWithDependencies = configurations.Any(config => config.Resolve<ConfigurationDependencyResolver>().Dependents.Any());

			var isAuthorized = args.SecurityState.IsAllowed;

			if (!hasSerializedItems)
			{
				yield return new GlobalWarnings(hasAllRootParentPaths, anyConfigurationsWithDependencies);
			}

			if (isAuthorized)
			{
				if (configurations.Length == 0)
				{
					yield return new NoConfigurations();
					yield break;
				}

				if (allowMultiSelect)
				{
					yield return new BatchProcessingControls(allowReserializeAll);
				}

				yield return new Literal(@"
						<article>
							<h2{0} Configurations</h2>".FormatWith(allowMultiSelect ? @" class=""fakebox fakebox-all""><span></span>" : ">"));

				if (allowMultiSelect)
				{
					yield return new Literal(@"<p class=""help"">Check 'Configurations' above to select all configurations, or individually select as many as you like below.</p>");

					if (!allowReserializeAll)
					{
						yield return new Literal(@"<p class=""help"">'Reserialize All' has been disabled because one or more configurations rely on root paths that do not exist in Sitecore currently.<br />'Reserialize' has also been removed from the configurations involved.</p>");
					}
				}
				else
				{
					yield return new Literal(@"<p class=""help"">Some configurations prevent the 'sync all' checkbox because they include predicates that rely on (currently) invalid root paths. You likely need to sync one or more base configurations.<p>");
				}



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
