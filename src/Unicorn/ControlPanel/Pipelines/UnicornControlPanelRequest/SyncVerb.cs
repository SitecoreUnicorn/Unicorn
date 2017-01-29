using System;
using System.Linq;
using System.Text;
using System.Web;
using Kamsar.WebConsole;
using Sitecore.Pipelines;
using Unicorn.Configuration;
using Unicorn.ControlPanel.Headings;
using Unicorn.ControlPanel.Responses;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncEnd;
using Unicorn.Predicates;
using Sitecore.Diagnostics;
using Unicorn.Data.DataProvider;
using Unicorn.Data.Dilithium;
using Unicorn.Pipelines.UnicornOperationStart;

// ReSharper disable RedundantArgumentNameForLiteralExpression
// ReSharper disable RedundantArgumentName

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class SyncVerb : UnicornControlPanelRequestPipelineProcessor
	{
		public SyncVerb() : this("Sync")
		{
		}

		protected SyncVerb(string verb) : base(verb)
		{

		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new WebConsoleResponse("Sync Unicorn", args.SecurityState.IsAutomatedTool, new HeadingService(), progress => Process(progress, new WebConsoleLogger(progress, args.Context.Request.QueryString["log"])));
		}

		protected virtual void Process(IProgressStatus progress, ILogger additionalLogger)
		{
			var configurations = ResolveConfigurations();
			int taskNumber = 1;

			bool success = true;

			try
			{
				var startArgs = new UnicornOperationStartPipelineArgs(OperationType.FullSync, configurations, additionalLogger, null);
				CorePipeline.Run("unicornSyncStart", startArgs);

				foreach (var configuration in configurations)
				{
					var logger = configuration.Resolve<ILogger>();
					var helper = configuration.Resolve<SerializationHelper>();

					using (new LoggingContext(additionalLogger, configuration))
					{
						try
						{
							var startStatement = new StringBuilder();
							startStatement.Append(configuration.Name);
							startStatement.Append(" is being synced");

							if (configuration.EnablesDilithium())
							{
								startStatement.Append(" with Dilithium");
								if (configuration.EnablesDilithiumSql()) startStatement.Append(" SQL");
								if (configuration.EnablesDilithiumSql() && configuration.EnablesDilithiumSfs()) startStatement.Append(" +");
								if (configuration.EnablesDilithiumSfs()) startStatement.Append(" Serialized");
								startStatement.Append(" enabled.");
							}
							else
							{
								startStatement.Append(".");
							}

							logger.Info(startStatement.ToString());

							using (new TransparentSyncDisabler())
							{
								var predicate = configuration.Resolve<IPredicate>();

								var roots = predicate.GetRootPaths();

								var index = 0;
								helper.SyncTree(
									configuration: configuration,
									rootLoadedCallback: item =>
									{
										WebConsoleUtility.SetTaskProgress(progress, taskNumber, configurations.Length, (int) ((index / (double) roots.Length) * 100));
										index++;
									},
									runSyncStartPipeline: false
								);
							}
						}
						catch (DeserializationSoftFailureAggregateException ex)
						{
							logger.Error(ex);
							// allow execution to continue, because the exception was non-fatal
						}
						catch (Exception ex)
						{
							logger.Error(ex);
							success = false;
							break;
						}
					}

					taskNumber++;
				}
			}
			finally
			{
				ReactorContext.Dispose();
			}

			try
			{
				CorePipeline.Run("unicornSyncEnd", new UnicornSyncEndPipelineArgs(progress, success, configurations));
			}
			catch (Exception exception)
			{
				Log.Error("Error occurred in unicornSyncEnd pipeline.", exception);
				progress.ReportException(exception);
			}
		}

		protected virtual IConfiguration[] ResolveConfigurations()
		{
			var config = HttpContext.Current.Request.QueryString["configuration"];
			var targetConfigurations = ControlPanelUtility.ResolveConfigurationsFromQueryParameter(config);

			if (targetConfigurations.Length == 0) throw new ArgumentException("Configuration(s) requested were not defined.");

			var skipTransparent = HttpContext.Current.Request.QueryString["skipTransparentConfigs"];
			if (skipTransparent == "1")
			{
				targetConfigurations = targetConfigurations.Where(configuration => !configuration.Resolve<IUnicornDataProviderConfiguration>().EnableTransparentSync).ToArray();

				if (targetConfigurations.Length == 0) Log.Warn("[Unicorn] All configurations were transparent sync and skipTransparentConfigs was active. Syncing nothing.", this);
			}

			return targetConfigurations;
		}
	}
}
