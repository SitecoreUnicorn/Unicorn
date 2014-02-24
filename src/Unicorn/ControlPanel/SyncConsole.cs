using Kamsar.WebConsole;
using Unicorn.Dependencies;
using Unicorn.Loader;
using Unicorn.Logging;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Runs a Unicorn sync using current DI configuration in a WebConsole, and logs it to the Sitecore log as well
	/// </summary>
	public class SyncConsole : ControlPanelConsole
	{
		private readonly IConfiguration[] _configurations;

		public SyncConsole(bool isAutomatedTool, IConfiguration[] configurations) : base(isAutomatedTool)
		{
			_configurations = configurations;
		}

		protected override string Title
		{
			get { return "Sync Unicorn"; }
		}

		protected override void Process(IProgressStatus progress)
		{
			foreach (var configuration in _configurations)
			{
				using (new LoggingContext(new WebConsoleLogger(progress), configuration))
				{
					progress.ReportStatus("Processing Unicorn configuration " + configuration.Name);

					var loader = configuration.Resolve<SerializationLoader>();

					loader.LoadAll(configuration);
				}
			}
		}
	}
}
