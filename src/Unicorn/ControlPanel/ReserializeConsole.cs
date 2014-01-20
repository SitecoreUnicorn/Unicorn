using Kamsar.WebConsole;
using Unicorn.Data;
using Unicorn.Dependencies;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.ControlPanel
{
	public class ReserializeConsole : ControlPanelConsole
	{
		private readonly IDependencyRegistry _dependencyRegistry;

		public ReserializeConsole(bool isAutomatedTool, IDependencyRegistry dependencyRegistry) : base(isAutomatedTool)
		{
			_dependencyRegistry = dependencyRegistry;
		}

		protected override string Title
		{
			get { return "Reserialize Unicorn"; }
		}

		protected override void Process(IProgressStatus progress)
		{
			// tell the Unicorn DI container to wire to the console for its progress logging
			_dependencyRegistry.Register(() => progress);

			var predicate = _dependencyRegistry.Resolve<IPredicate>();
			var serializationProvider = _dependencyRegistry.Resolve<ISerializationProvider>();

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
