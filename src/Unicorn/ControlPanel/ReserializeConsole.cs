using Kamsar.WebConsole;
using Unicorn.Data;
using Unicorn.Dependencies;
using Unicorn.Logging;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.ControlPanel
{
	public class ReserializeConsole : ControlPanelConsole
	{
		private readonly IConfiguration[] _configurations;

		public ReserializeConsole(bool isAutomatedTool, IConfiguration[] configurations) : base(isAutomatedTool)
		{
			_configurations = configurations;
		}

		protected override string Title
		{
			get { return "Reserialize Unicorn"; }
		}

		protected override void Process(IProgressStatus progress)
		{
			foreach (var configuration in _configurations)
			{
				using (new LoggingContext(new WebConsoleLogger(progress), configuration))
				{
					progress.ReportStatus("Processing Unicorn configuration " + configuration.Name);

					var predicate = configuration.Resolve<IPredicate>();
					var serializationProvider = configuration.Resolve<ISerializationProvider>();

					var roots = predicate.GetRootItems();

					int index = 1;
					foreach (var root in roots)
					{
						var rootReference = serializationProvider.GetReference(root);
						if (rootReference != null)
						{
							progress.ReportStatus("[D] existing serialized items under {0}", MessageType.Warning, rootReference.DisplayIdentifier);
							rootReference.Delete();
						}

						progress.ReportStatus("[U] Serializing included items under root {0}", MessageType.Info, root.DisplayIdentifier);
						Serialize(root, predicate, serializationProvider, progress);
						progress.Report((int) ((index/(double) roots.Length)*100));
						index++;
					}
				}
			}
		}

		private void Serialize(ISourceItem root, IPredicate predicate, ISerializationProvider serializationProvider, IProgressStatus progress)
		{
			var predicateResult = predicate.Includes(root);
			if (predicateResult.IsIncluded)
			{
				serializationProvider.SerializeItem(root);

				foreach (var child in root.Children)
				{
					Serialize(child, predicate, serializationProvider, progress);
				}
			}
			else
			{
				progress.ReportStatus("[S] {0} because {1}", MessageType.Warning, root.DisplayIdentifier, predicateResult.Justification);
			}
		}
	}
}
