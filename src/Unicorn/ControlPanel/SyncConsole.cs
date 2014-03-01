using System;
using System.Linq;
using System.Web;
using Kamsar.WebConsole;
using Unicorn.Configuration;
using Unicorn.Loader;
using Unicorn.Logging;

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
			foreach (var configuration in ResolveConfigurations())
			{
				var logger = configuration.Resolve<ILogger>();
				using (new LoggingContext(new WebConsoleLogger(progress), configuration))
				{
					logger.Info("Control Panel Sync: Processing Unicorn configuration " + configuration.Name);

					var loader = configuration.Resolve<SerializationLoader>();

					loader.LoadAll(configuration);

					logger.Info("Control Panel Sync: Completed syncing Unicorn configuration " + configuration.Name);
				}
			}
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
