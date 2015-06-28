using System;
using System.Linq;
using System.Web;
using Rainbow.Model;
using Rainbow.Storage;
using Kamsar.WebConsole;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data;
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
					try
					{
						logger.Info("Control Panel Reserialize: Processing Unicorn configuration " + configuration.Name);

						var predicate = configuration.Resolve<IPredicate>();
						var serializationStore = configuration.Resolve<ITargetDataStore>();
						var sourceStore = configuration.Resolve<ISourceDataStore>();

						var roots = configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems();

						int index = 1;
						foreach (var root in roots)
						{
							var rootReference = serializationStore.GetById(root.Id, root.DatabaseName);
							if (rootReference != null)
							{
								logger.Warn("[D] existing serialized items under {0}".FormatWith(rootReference.GetDisplayIdentifier()));
								// this doesn't really account for excluded children - it just nukes everything.
								// ideally it would leave excluded serialized items alone.
								serializationStore.Remove(rootReference.Id, rootReference.DatabaseName);
							}

							logger.Info("[U] Serializing included items under root {0}".FormatWith(root.GetDisplayIdentifier()));
							Serialize(root, predicate, serializationStore, sourceStore, logger);
							progress.Report((int) ((index/(double) roots.Length)*100));
							index++;
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

		private void Serialize(ISerializableItem root, IPredicate predicate, ITargetDataStore serializationStore, ISourceDataStore sourceDataStore, ILogger logger)
		{
			var predicateResult = predicate.Includes(root);
			if (predicateResult.IsIncluded)
			{
				serializationStore.Save(root);

				foreach (var child in sourceDataStore.GetChildren(root.Id, root.DatabaseName))
				{
					Serialize(child, predicate, serializationStore, sourceDataStore, logger);
				}
			}
			else
			{
				logger.Warn("[S] {0} because {1}".FormatWith(root.GetDisplayIdentifier(), predicateResult.Justification));
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
