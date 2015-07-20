using System.Collections.Generic;
using Rainbow.Storage;
using Sitecore.Data.Serialization.Presets;

namespace Unicorn.Predicates
{
	public class PresetTreeRoot : TreeRoot
	{
		public PresetTreeRoot(string name, string path, string databaseName, IList<ExcludeEntry> exclude) : base(name, path, databaseName)
		{
			Exclude = exclude;

			if (name == null) Name = path.Substring(path.LastIndexOf('/') + 1);
		}

		public IList<ExcludeEntry> Exclude { get; protected set; }

		public void Rename(string newName)
		{
			Name = newName;
		}
	}
}
