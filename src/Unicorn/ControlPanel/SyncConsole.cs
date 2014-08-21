using System;
using System.Linq;
using System.Web;
using Kamsar.WebConsole;
using Sitecore.Pipelines;
using Unicorn.Configuration;
using Unicorn.Evaluators;
using Unicorn.Loader;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncBegin;
using Unicorn.Pipelines.UnicornSyncComplete;
using Unicorn.Pipelines.UnicornSyncEnd;
using Unicorn.Predicates;
using Unicorn.Publishing;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Runs a Unicorn sync in a WebConsole of a configuration or configurations
	/// </summary>
	public class SyncConsole : ControlPanelConsole
	{
		private readonly IConfiguration[] _configurations;

		public SyncConsole(bool isAutomatedTool, IConfiguration[] configurations)
			: base(isAutomatedTool)
		{
			_configurations = configurations;
		}

		protected override string Title
		{
			get { return "Sync Unicorn"; }
		}

		protected override void Process(IProgressStatus progress)
		{
			var configurations = ResolveConfigurations();

			foreach (var configuration in configurations)
			{
				var logger = configuration.Resolve<ILogger>();

				using (new LoggingContext(new WebConsoleLogger(progress), configuration))
				{
					try
					{
						logger.Info("Control Panel Sync: Processing Unicorn configuration " + configuration.Name);

						CorePipeline.Run("unicornSyncBegin", new UnicornSyncBeginPipelineArgs(configuration));

						var pathResolver = configuration.Resolve<PredicateRootPathResolver>();
						var retryer = configuration.Resolve<IDeserializeFailureRetryer>();
						var consistencyChecker = configuration.Resolve<IConsistencyChecker>();
						var loader = configuration.Resolve<SerializationLoader>();

						var roots = pathResolver.GetRootSerializedItems();

						var index = 0;

						loader.LoadAll(roots, retryer, consistencyChecker, item =>
						{
							progress.Report((int)(((index + 1) / (double)roots.Length) * 100));
							index++;
						});

						CorePipeline.Run("unicornSyncComplete", new UnicornSyncCompletePipelineArgs(configuration));

						logger.Info("Control Panel Sync: Completed syncing Unicorn configuration " + configuration.Name);
					}
					catch (Exception ex)
					{
						logger.Error(ex);
						break;
					}
				}
			}

			CorePipeline.Run("unicornSyncEnd", new UnicornSyncEndPipelineArgs(configurations));
		}

		protected virtual IConfiguration[] ResolveConfigurations()
		{
			var config = HttpContext.Current.Request.QueryString["configuration"];

			if (string.IsNullOrWhiteSpace(config)) return _configurations;

			var targetConfiguration = _configurations.FirstOrDefault(x => x.Name == config);

			if (targetConfiguration == null) throw new ArgumentException("Configuration requested was not defined.");

			return new[] { targetConfiguration };
		}
	}
}
