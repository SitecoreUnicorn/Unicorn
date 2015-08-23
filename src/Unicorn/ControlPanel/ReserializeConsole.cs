using System;
using System.Linq;
using System.Web;
using Kamsar.WebConsole;
using Unicorn.Configuration;
using Unicorn.ControlPanel.Headings;
using Unicorn.Logging;
using Unicorn.Predicates;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Renders a WebConsole that handles reserialize - or initial serialize - for Unicorn configurations
	/// </summary>
	public class ReserializeConsole : ControlPanelConsole
	{
		private readonly IConfiguration[] _configurations;

		public ReserializeConsole(bool isAutomatedTool, IConfiguration[] configurations)
			: base(isAutomatedTool, new HeadingService())
		{
			_configurations = configurations;
		}

		protected override string Title
		{
			get { return "Reserialize Unicorn"; }
		}

		protected override void Process(IProgressStatus progress)
		{
			foreach (var configuration in ResolveConfigurations())
			{
				var logger = configuration.Resolve<ILogger>();

				using (new LoggingContext(new WebConsoleLogger(progress), configuration))
				{
					try
					{
						logger.Info("Control Panel Reserialize: Processing Unicorn configuration " + configuration.Name);

						using (new TransparentSyncDisabler())
						{
							var helper = configuration.Resolve<SerializationHelper>();

							var roots = configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems();

							int index = 1;
							foreach (var root in roots)
							{
								helper.DumpTree(root);
								progress.Report((int) ((index/(double) roots.Length)*100));
								index++;
							}
						}

						logger.Info("Control Panel Reserialize: Finished reserializing Unicorn configuration " + configuration.Name);
					}
					catch (Exception ex)
					{
						logger.Error(ex);
						break;
					}
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
