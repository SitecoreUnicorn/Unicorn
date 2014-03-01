using System;
using System.Linq;
using System.Web;
using Kamsar.WebConsole;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Renders a WebConsole that handles reserialize - or initial serialize - for Unicorn configurations
	/// </summary>
	public class ReserializeConsole : ControlPanelConsole
	{
		private readonly IConfiguration[] _configurations;

		public ReserializeConsole(bool isAutomatedTool, IConfiguration[] configurations)
			: base(isAutomatedTool)
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
					logger.Info("Control Panel Reserialize: Processing Unicorn configuration " + configuration.Name);

					var predicate = configuration.Resolve<IPredicate>();
					var serializationProvider = configuration.Resolve<ISerializationProvider>();

					var roots = predicate.GetRootItems();

					int index = 1;
					foreach (var root in roots)
					{
						var rootReference = serializationProvider.GetReference(root);
						if (rootReference != null)
						{
							logger.Warn("[D] existing serialized items under {0}".FormatWith(rootReference.DisplayIdentifier));
							rootReference.Delete();
						}

						logger.Info("[U] Serializing included items under root {0}".FormatWith(root.DisplayIdentifier));
						Serialize(root, predicate, serializationProvider, logger);
						progress.Report((int)((index / (double)roots.Length) * 100));
						index++;
					}

					logger.Info("Control Panel Reserialize: Finished reserializing Unicorn configuration " + configuration.Name);
				}
			}
		}

		private void Serialize(ISourceItem root, IPredicate predicate, ISerializationProvider serializationProvider, ILogger logger)
		{
			var predicateResult = predicate.Includes(root);
			if (predicateResult.IsIncluded)
			{
				serializationProvider.SerializeItem(root);

				foreach (var child in root.Children)
				{
					Serialize(child, predicate, serializationProvider, logger);
				}
			}
			else
			{
				logger.Warn("[S] {0} because {1}".FormatWith(root.DisplayIdentifier, predicateResult.Justification));
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
