using System;
using System.Diagnostics;
using System.Web;
using Kamsar.WebConsole;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.ControlPanel.Headings;
using Unicorn.ControlPanel.Responses;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Predicates;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class ReserializeVerb : UnicornControlPanelRequestPipelineProcessor
	{
		public ReserializeVerb() : this("Reserialize")
		{
		}

		protected ReserializeVerb(string verb) : base(verb)
		{
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new WebConsoleResponse("Reserialize Unicorn", args.SecurityState.IsAutomatedTool, new HeadingService(), progress => Process(progress, new WebConsoleLogger(progress)));
		}

		protected virtual void Process(IProgressStatus progress, ILogger additionalLogger)
		{
			var configurations = ResolveConfigurations();
			int taskNumber = 1;

			foreach (var configuration in configurations)
			{
				var logger = configuration.Resolve<ILogger>();

				using (new LoggingContext(additionalLogger, configuration))
				{
					try
					{
						var timer = new Stopwatch();
						timer.Start();

						logger.Info(string.Empty);
						logger.Info(configuration.Name + " is being reserialized.");

						using (new TransparentSyncDisabler())
						{
							var targetDataStore = configuration.Resolve<ITargetDataStore>();
							var helper = configuration.Resolve<SerializationHelper>();

							// nuke any existing items in the store before we begin. This is a full reserialize so we want to
							// get rid of any existing stuff even if it's not part of existing configs
							logger.Warn("[D] Clearing existing items from {0} (if any)".FormatWith(targetDataStore.FriendlyName));
							targetDataStore.Clear();

							var roots = configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems();

							int index = 1;
							foreach (var root in roots)
							{
								helper.DumpTree(root, configuration);
								WebConsoleUtility.SetTaskProgress(progress, taskNumber, configurations.Length, (int)((index / (double)roots.Length) * 100));
								index++;
							}
						}

						timer.Stop();

						logger.Info("{0} reserialization complete in {1}ms.".FormatWith(configuration.Name, timer.ElapsedMilliseconds));
					}
					catch (Exception ex)
					{
						logger.Error(ex);
						break;
					}

					taskNumber++;
				}
			}
		}

		protected virtual IConfiguration[] ResolveConfigurations()
		{
			var config = HttpContext.Current.Request.QueryString["configuration"];
			var targetConfigurations = ControlPanelUtility.ResolveConfigurationsFromQueryParameter(config);

			if (targetConfigurations.Length == 0) throw new ArgumentException("Configuration(s) requested were not defined.");

			return targetConfigurations;
		}
	}
}
