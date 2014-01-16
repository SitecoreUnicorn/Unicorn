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
		public SyncConsole(bool isAutomatedTool) : base(isAutomatedTool)
		{
			
		}

		protected override string Title
		{
			get { return "Sync Unicorn"; }
		}

		protected override void Process(IProgressStatus progress)
		{
			// tell the Unicorn DI container to wire to the console for its progress logging
			Registry.Current.RegisterInstanceFactory(() => progress);

			var loader = new SerializationLoader();

			loader.LoadAll();
		}
	}
}
