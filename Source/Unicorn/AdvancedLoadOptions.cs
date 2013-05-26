using Kamsar.WebConsole;
using Sitecore.Data.Serialization;
using Unicorn.Predicates;

namespace Unicorn
{
	/// <summary>
	/// Extends the stock serialization LoadOptions to enable:
	/// * Preset support (eg. load only one preset include with its excludes respected)
	/// * Progress reporting support with the progress pattern
	/// * DeleteOrphans support (allows deletes to occur WITHOUT forcing an update everywhere)
	/// </summary>
	public class AdvancedLoadOptions : LoadOptions
	{
		public AdvancedLoadOptions(IPredicate preset)
		{
			DeleteOrphans = false;
			Progress = new StringProgressStatus();
			Preset = preset;
		}

		/// <summary>
		/// A progress implementation the loader can use to report progress of loading serialized items
		/// </summary>
		public IProgressStatus Progress { get; set; }

		/// <summary>
		/// The serialization preset we'll be loading with the loader
		/// </summary>
		public IPredicate Preset { get; set; }

		/// <summary>
		/// If true items not present on disk will be deleted (subset of ForceUpdate behavior) - update behavior remains non-forced
		/// </summary>
		public bool DeleteOrphans { get; set; }
	}
}
