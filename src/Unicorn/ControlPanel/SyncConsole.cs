using Kamsar.WebConsole;
using Unicorn.Dependencies;
using Unicorn.Loader;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Runs a Unicorn sync using current DI configuration in a WebConsole, and logs it to the Sitecore log as well
	/// </summary>
	public class SyncConsole : ControlPanelConsole
	{
		private readonly IDependencyRegistry _dependencyRegistry;

		public SyncConsole(bool isAutomatedTool, IDependencyRegistry dependencyRegistry) : base(isAutomatedTool)
		{
			_dependencyRegistry = dependencyRegistry;
		}

		protected override string Title
		{
			get { return "Sync Unicorn"; }
		}

		protected override void Process(IProgressStatus progress)
		{
			// tell the Unicorn DI container to wire to the console for its progress logging
			_dependencyRegistry.Register(() => progress);

			var loader = _dependencyRegistry.Resolve<SerializationLoader>();

			loader.LoadAll(_dependencyRegistry);
		}
	}
}
