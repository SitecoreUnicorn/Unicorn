using System.Collections.Generic;
using Rainbow.Storage;

namespace Unicorn.Predicates
{
	public class PresetTreeRoot : TreeRoot
	{
		public PresetTreeRoot(string name, string path, string databaseName) : base(name, path, databaseName)
		{
			Exclusions = new List<IPresetTreeExclusion>();

			if (name == null) Name = path.Substring(path.LastIndexOf('/') + 1);
		}

		public PresetTreeRoot(string name, string path, string databaseName, string namePattern) : base(name, path, databaseName, namePattern)
		{
			Exclusions = new List<IPresetTreeExclusion>();

			if (name == null) Name = path.Substring(path.LastIndexOf('/') + 1);
		}

		public IList<IPresetTreeExclusion> Exclusions { get; set; }
	}
}
