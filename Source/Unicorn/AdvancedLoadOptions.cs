using System;
using Kamsar.WebConsole;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;

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
		public AdvancedLoadOptions(string path) : this(new IncludeEntry(path))
		{
			
		}

		public AdvancedLoadOptions(IncludeEntry preset)
		{
			DeleteOrphans = false;
			Progress = new FakeProgress();
			Preset = preset;
		}

		/// <summary>
		/// A progress implementation the loader can use to report progress of loading serialized items
		/// </summary>
		public IProgressStatus Progress { get; set; }

		/// <summary>
		/// The serialization preset we'll be loading with the loader
		/// </summary>
		public IncludeEntry Preset { get; set; }

		/// <summary>
		/// If true items not present on disk will be deleted (subset of ForceUpdate behavior) - update behavior remains non-forced
		/// </summary>
		public bool DeleteOrphans { get; set; }

		/// <summary>
		/// Fake implementation of IProgressStatus that's used by default so if you dont provide a progress implementation it won't be null.
		/// </summary>
		private class FakeProgress : IProgressStatus
		{
			public void Report(int percent)
			{
				
			}

			public void Report(int percent, string statusMessage)
			{
				
			}

			public void Report(int percent, string statusMessage, MessageType type)
			{
				
			}

			public void ReportException(Exception exception)
			{
				
			}

			public void ReportStatus(string statusMessage)
			{
				
			}

			public void ReportStatus(string statusMessage, MessageType type)
			{
				
			}
		}

	}
}
