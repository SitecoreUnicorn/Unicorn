using System;
using System.Linq;
using Sitecore;
using Sitecore.Globalization;
using Unicorn.Logging;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	/// <summary>
	/// Clears the dictionary if a dictionary entry was part of a sync
	/// </summary>
	public class DictionaryCacheClearer : IUnicornSyncCompleteProcessor
	{
		public void Process(UnicornSyncCompletePipelineArgs args)
		{
			var targetId = TemplateIDs.DictionaryEntry.Guid;

			if (args.Changes.Any(item => (item.TemplateId ?? Guid.Empty).Equals(targetId)))
			{
				var log = args.Configuration.Resolve<ILogger>();

				log.Debug(string.Empty);
				log.Debug("> Dictionary entries were altered during sync. Clearing dictionary caches...");
				Translate.ResetCache();
			}
		}
	}
}
